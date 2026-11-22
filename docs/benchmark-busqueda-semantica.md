# Benchmark de búsqueda semántica — asistente RAG

Este documento registra la estrategia de recuperación de lugares para el asistente RAG y el benchmark ejecutado contra la base local.

## Objetivo

Definir cómo se combinan:

- filtros duros en SQL;
- filtro por radio con PostGIS;
- orden vectorial con pgvector;
- umbral opcional de similitud.

La prioridad es que el asistente reciba lugares relevantes y que el plan de Postgres sea explícito antes de sumar intención y LLM.

## Dataset actual

| Métrica | Valor |
|---|---|
| Lugares totales | 199 |
| Lugares con embedding | 199 |
| Dimensiones | 1024 (`BAAI/bge-m3`) |
| Tabla | `lugares` |
| Columna vectorial | `"Embedding"` (`vector(1024)`) |
| Índice vectorial | `IX_lugares_Embedding` (`hnsw`, `vector_cosine_ops`) |
| Columna espacial | `Ubicacion` (`geography (point, 4326)`) |
| Índice espacial | `IX_lugares_Ubicacion` (`gist`) |

## Estrategia implementada

La consulta se construye en `LugarSemanticaQuery`:

1. Filtro base: `activo = true` y `Embedding IS NOT NULL`.
2. Filtros duros en SQL:
   - barrio;
   - categorías;
   - precio máximo;
   - aire libre / bajo techo;
   - apto niños;
   - accesibilidad;
   - restricción dietaria;
   - IDs excluidos.
3. Radio:
   - `Ubicacion.IsWithinDistance(punto, radioMetros)`;
   - como la columna es `geography`, la distancia se interpreta en metros;
   - el punto se construye como `(longitud, latitud)` con SRID 4326.
4. Orden vectorial:
   - `Embedding <=> queryVector`;
   - índice `vector_cosine_ops`;
   - `ORDER BY distancia LIMIT TopK`.
5. Umbral opcional:
   - se aplica **después** del `ORDER BY` + `LIMIT`, en el `WHERE` externo;
   - evita agregar una condición extra al camino principal cuando no se configura;
   - si no hay resultados que superen el umbral, se activa el fallback en la capa de aplicación.

## Resultados del benchmark

Consulta con un embedding real de un lugar de Palermo.

| Escenario | Plan de Postgres | Filas | Tiempo observado |
|---|---|---|---|
| Sin filtros duros | Seq Scan + top-N sort | 199 → 12 | ~1.9 ms warm; ~58 ms cold |
| Barrio Palermo | Seq Scan sobre 51 filas + top-N sort | 51 → 12 | ~0.35 ms |
| Radio 800 m | Bitmap Index Scan `IX_lugares_Ubicacion` + Bitmap Heap Scan | 24 → 12 | ~11.6 ms |
| Barrio + radio + umbral 0.45 | BitmapAnd (`IX_lugares_Ubicacion` + `idx_lugares_filtro_catalogo`) + subquery con umbral | 12 | ~0.46 ms |

Observaciones:

- Con 199 lugares, Postgres **no usa** el índice HNSW: elige scan exacto y top-N sort.
- El índice GiST de PostGIS sí se usa para el filtro por radio.
- El umbral queda en el `WHERE` externo y no impide el `ORDER BY ... LIMIT` interno.
- La latencia varía bastante entre la primera ejecución y las siguientes por caché de datos y planes.

### Distribución de similitud con embedding de lugar

Consulta de Palermo contra lugares de Palermo dentro de 800 m, usando el embedding de un lugar como query:

| Percentil | Similitud |
|---|---|
| p50 | 0.619 |
| p75 | 0.631 |
| p90 | 0.636 |
| máximo | 0.694 |

Esta medición sirve para validar el SQL, pero no representa consultas libres: un embedding de lugar se parece demasiado a los documentos del mismo catálogo.

### Calibración con consultas reales (Fase 4)

Se generaron embeddings de consultas representativas con `POST /embeddings/queries` del servicio `touringgo-embeddings` y se midió la similitud de los top 12 lugares de Palermo (51 lugares).

| Consulta | min | avg | max | ≥ 0.40 | ≥ 0.45 | ≥ 0.50 |
|---|---:|---:|---:|---:|---:|---:|
| café de especialidad con buena onda | 0.524 | 0.545 | 0.637 | 12 | 12 | 12 |
| museo de arte | 0.353 | 0.378 | 0.512 | 1 | 1 | 1 |
| parque o plaza al aire libre | 0.508 | 0.538 | 0.565 | 12 | 12 | 12 |
| bar para tomar algo de noche | 0.509 | 0.542 | 0.581 | 12 | 12 | 12 |
| restaurante para comer | 0.519 | 0.532 | 0.554 | 12 | 12 | 12 |
| librería de diseño | 0.414 | 0.435 | 0.582 | 12 | 2 | 1 |
| lugar para ir con niños | 0.497 | 0.502 | 0.514 | 12 | 12 | 7 |
| opción vegana | 0.417 | 0.427 | 0.460 | 12 | 1 | 0 |
| plan romántico | 0.302 | 0.314 | 0.356 | 0 | 0 | 0 |
| algo tranquilo y bajo techo | 0.472 | 0.477 | 0.490 | 12 | 12 | 0 |

Lectura:

- `0.60–0.65` es demasiado alto para consultas libres: descarta casi todos los resultados.
- `0.50` elimina consultas amplias como “algo tranquilo y bajo techo” y deja sin resultados a “opción vegana”.
- `0.45` conserva resultados en consultas amplias y filtra ruido en consultas específicas.
- Cuando quedan menos de `MinResultados`, el asistente activa el fallback al catálogo filtrado.

**Umbral elegido: `0.45`.**

Se aplica después del `ORDER BY` + `LIMIT` y antes de decidir el fallback.

## Cómo reproducir

Conectar a la base local:

```powershell
docker exec -it touringgo-infraestructura-lugares-db-1 psql -U postgres -d lugares_db
```

Verificar catálogo:

```sql
SELECT count(*) AS total, count("Embedding") AS con_embedding
FROM lugares;
```

Preparar un vector de consulta:

```sql
CREATE TEMP TABLE _q AS
SELECT "Embedding" AS v
FROM lugares
WHERE "Embedding" IS NOT NULL
  AND barrio = 'palermo'
LIMIT 1;

ANALYZE _q;
```

Plan base:

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT l.id, l."Embedding" <=> q.v AS distancia
FROM lugares l, _q q
WHERE l.activo AND l."Embedding" IS NOT NULL
ORDER BY l."Embedding" <=> q.v
LIMIT 12;
```

Plan con barrio y radio:

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT l.id, l."Embedding" <=> q.v AS distancia
FROM lugares l, _q q
WHERE l.activo AND l."Embedding" IS NOT NULL
  AND l.barrio = 'palermo'
  AND ST_DWithin(
      l."Ubicacion",
      ST_SetSRID(ST_MakePoint(-58.4280328, -34.5912554), 4326)::geography,
      800
  )
ORDER BY l."Embedding" <=> q.v
LIMIT 12;
```

Plan con umbral:

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT t.id, t.distancia
FROM (
    SELECT l.id, l."Embedding" <=> q.v AS distancia
    FROM lugares l, _q q
    WHERE l.activo AND l."Embedding" IS NOT NULL
      AND l.barrio = 'palermo'
    ORDER BY l."Embedding" <=> q.v
    LIMIT 12
) t
WHERE 1.0 - t.distancia >= 0.60;
```

## Índice HNSW

El índice HNSW existe, pero no se usa con el catálogo actual. Si en el futuro el catálogo crece y Postgres empieza a elegirlo:

- medir con `EXPLAIN ANALYZE` antes de tocar parámetros;
- recordar que pgvector puede devolver menos de `TopK` cuando combina índice aproximado con filtros selectivos;
- evaluar iterative scan si la versión de pgvector es 0.8+;
- si se usa `hnsw.ef_search`, ejecutarlo con `SET LOCAL` dentro de una transacción explícita para no contaminar el pooling de conexiones:

```sql
BEGIN;
SET LOCAL hnsw.ef_search = 100;
-- consulta
COMMIT;
```

No agregar más perillas de configuración hasta que el benchmark demuestre que son necesarias.

## Criterio de salida

- [x] SQL del radio documentado.
- [x] Metros confirmados con `geography`.
- [x] `EXPLAIN ANALYZE` ejecutado con datos reales.
- [x] Estrategia elegida: scan exacto mientras el catálogo sea chico.
- [x] Umbral aplicado después del `ORDER BY` + `LIMIT`.
- [x] Distribución de similitud registrada.
- [x] Umbral calibrado con consultas reales: `0.45`.
- [x] `Asistente:SimilitudMinima=0.45` aplicado en Itinerarios.
- [ ] Repetir el benchmark con un catálogo más grande o más consultas reales.

## Ver también

- [Embeddings y backfill del asistente RAG](embeddings-y-backfill.md)
