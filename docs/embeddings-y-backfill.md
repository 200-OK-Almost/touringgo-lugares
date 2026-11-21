# Embeddings y backfill — asistente RAG

Este documento describe cómo se generan, versionan y mantienen los embeddings de lugares que usa el asistente RAG.

## Modelo

- Modelo: `BAAI/bge-m3`.
- Dimensiones: `1024`.
- Servicio: `touringgo-embeddings`.
- Persistencia: columna `lugares."Embedding"` de tipo `vector(1024)`.
- Índice: `IX_lugares_Embedding` (`hnsw`, `vector_cosine_ops`).

El texto fuente se construye con `BgeM3LugarEmbeddingTextBuilder` e incluye:

- nombre;
- tipo de lugar;
- categorías;
- barrio y subzona;
- descripción;
- características (aire libre, bajo techo, niños, accesibilidad, menú infantil);
- opciones dietarias.

## Configuración

```json
{
  "Embeddings": {
    "BaseUrl": "http://localhost:8084/",
    "Model": "BAAI/bge-m3",
    "TextBuilderVersion": "v1",
    "Backfill": {
      "Enabled": true,
      "IntervalSeconds": 300,
      "BatchSize": 32,
      "MaxBackoffSeconds": 3600,
      "SampleSize": 20,
      "SimilarityThreshold": 0.999
    }
  }
}
```

| Clave | Descripción |
|---|---|
| `Embeddings:BaseUrl` | URL del servicio de embeddings. |
| `Embeddings:Model` | Modelo usado en el hash del texto fuente. |
| `Embeddings:TextBuilderVersion` | Versión del template de texto. Cambiarla fuerza re-embedding. |
| `Embeddings:Backfill:Enabled` | Activa o desactiva el hosted service. |
| `Embeddings:Backfill:IntervalSeconds` | Intervalo entre ejecuciones exitosas. |
| `Embeddings:Backfill:BatchSize` | Cantidad máxima de lugares por ejecución y tamaño de lote de embedding. |
| `Embeddings:Backfill:MaxBackoffSeconds` | Tope del backoff ante fallos. |
| `Embeddings:Backfill:SampleSize` | Cantidad de lugares a muestrear antes de confiar en embeddings existentes. |
| `Embeddings:Backfill:SimilarityThreshold` | Similitud mínima del muestreo. Valor actual: `0.999`. |

## Hash versionado

Cada lugar guarda:

- `embedding_source_hash`: SHA-256 hexadecimal del texto fuente versionado.
- `embedding_updated_at`: fecha de la última generación o verificación.

El hash se calcula sobre:

```text
modelo
versión del text builder
texto fuente completo
```

Consecuencias:

- si cambia el texto del lugar, cambia el hash y se re-embebe;
- si cambia el template, cambia la versión y se re-embebe todo;
- si cambia el modelo, cambia el hash y se re-embebe todo;
- si el hash coincide, no se gasta una llamada al servicio de embeddings.

## Backfill

El backfill corre en `EmbeddingBackfillHostedService` y usa `LugarEmbeddingBackfillService`.

Características:

- se ejecuta al arrancar la API y luego cada `IntervalSeconds`;
- usa un lock de Postgres (`pg_try_advisory_lock`) para evitar trabajo duplicado entre instancias;
- procesa como máximo `BatchSize` lugares por ejecución;
- detecta:
  - lugares sin embedding;
  - lugares con hash nulo;
  - lugares sin fecha de embedding;
  - lugares con `fecha_actualizacion > embedding_updated_at`;
- guarda hash y fecha después de generar o verificar;
- aplica backoff exponencial si el servicio de embeddings falla.

### Muestreo antes de confiar en embeddings existentes

Cuando encuentra lugares con embedding pero sin hash, el servicio:

1. toma hasta `SampleSize` lugares;
2. re-embebe su texto actual;
3. compara la similitud coseno contra el vector guardado;
4. si todos superan `SimilarityThreshold` (`0.999`), acepta los embeddings existentes y solo completa el hash;
5. si alguno queda por debajo, re-embebe todos los lugares del lote.

Esto evita re-embeber todo el catálogo después de agregar el hash, pero detecta si el vector guardado no corresponde al texto o modelo actual.

## Flujo de actualización

```text
Lugar creado o modificado
  -> FechaActualizacion se actualiza
  -> Backfill lo detecta
  -> Calcula hash del texto fuente
  -> Si hash coincide: actualiza EmbeddingUpdatedAt
  -> Si hash no coincide o falta embedding: re-embebe
  -> Guarda Embedding, EmbeddingSourceHash y EmbeddingUpdatedAt
```

La importación desde Google Places y el seeder inicial también generan hash y fecha al crear embeddings nuevos.

## Observabilidad

Logs relevantes:

- cantidad de lugares procesados;
- cantidad de re-embeddings;
- muestreo fallido;
- fallo del servicio de embeddings con próximo backoff;
- backfill deshabilitado.

No se loguean descripciones ni textos completos.

## Diagnóstico manual

Ver lugares pendientes:

```sql
SELECT count(*) AS pendientes
FROM lugares
WHERE "Embedding" IS NULL
   OR embedding_source_hash IS NULL
   OR embedding_updated_at IS NULL
   OR fecha_actualizacion > embedding_updated_at;
```

Ver distribución de hashes:

```sql
SELECT count(*) FILTER (WHERE embedding_source_hash IS NULL) AS sin_hash,
       count(*) FILTER (WHERE "Embedding" IS NOT NULL) AS con_embedding,
       count(*) AS total
FROM lugares;
```

## Relación con el asistente RAG

- Un lugar sin embedding no aparece en la búsqueda semántica.
- El hash permite mantener el índice vectorial alineado con el catálogo.
- La búsqueda usa `Model` y `TextBuilderVersion` de forma indirecta: si cambian, el backfill re-embebe antes de que el asistente consulte.
- Para la estrategia de filtros, radio y umbral, ver [benchmark-busqueda-semantica.md](benchmark-busqueda-semantica.md).
