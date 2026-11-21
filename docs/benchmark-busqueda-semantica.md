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

### Distribución de similitud

Consulta de Palermo contra lugares de Palermo dentro de 800 m:

| Percentil | Similitud |
|---|---|
| p50 | 0.619 |
| p75 | 0.631 |
| p90 | 0.636 |
| máximo | 0.694 |

Conclusión provisional:

- Un umbral de `0.45` es demasiado bajo para este caso: deja pasar casi todo.
- Un rango inicial razonable para calibrar sería `0.60–0.65`.
- No fijar el umbral en producción hasta medir consultas reales de usuarios y no solo un embedding de referencia.

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
- [ ] Repetir el benchmark con un catálogo más grande o consultas reales de usuarios.
