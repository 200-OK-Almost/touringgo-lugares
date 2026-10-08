# Especificación y Arquitectura — Microservicio de Lugares (TouringGO / TuristIA)

Este documento centraliza la información del template base, la arquitectura general del sistema y el alcance específico del microservicio **Lugares** (`touringgo-lugares`).

---

## 1. Contexto General del Sistema (TuristIA)

TuristIA es un asistente de planificación urbana y salidas grupales en CABA compuesto por 3 microservicios principales con arquitectura **Database-per-Service**:

1. **Usuarios (`turistia_core` / `usuarios_db`):** Autenticación (Google OAuth), perfil, memoria persistente de onboarding (preferencias, restricciones dietarias, categorías de interés).
2. **Lugares (`turistia_geo` / `lugares_db`) — *[ESTE REPOSITORIO]*:** Catálogo de puntos de interés y alojamientos de CABA, combinación híbrida (seed curado + caché write-through de Google Places), control de cuota y filtros.
3. **Itinerarios (`itinerarios_db`):** Planificación, traducción de lenguaje natural mediante IA (Gemini), consulta/filtrado al servicio de Lugares, generación de paradas diarias y cálculo de rutas.
*(Nota: Finanzas/Gastos gestiona división de gastos exclusivamente para Modo Amigos).*

### Reglas de Integración entre Microservicios
* **Base de datos independiente:** Cada microservicio tiene su propia base de datos PostgreSQL (`lugares_db`).
* **Sin Foreign Keys físicas inter-servicio:** La vinculación se realiza exclusivamente mediante identificadores lógicos (`UUID`).
* **Integridad referencial física:** Se mantiene estricta dentro de cada microservicio (`CASCADE` o `RESTRICT` según corresponda).
* **Enums:** Se materializan como tipos nativos o strings/checks propios de cada servicio.

---

## 2. Alcance Específico de `touringgo-lugares`

### Responsabilidades
* Base de datos local con catálogo curado de CABA (30-50 lugares iniciales).
* Mecanismo de caché *write-through* con Google Places API (evitando duplicados mediante `id_google_place`).
* Control diario de consumo de la API de Google (Circuit Breaker con `control_cuota_google`).
* Filtros de catálogo: por barrio, categoría, indoor/outdoor (modo lluvia), apto niños, movilidad reducida y opciones dietarias.
* Exposición de endpoints REST para consulta y búsqueda de lugares consumibles por el microservicio de Itinerarios y la UI.

---

## 3. Modelo de Datos y DER (Subconjunto Lugares)

### Enums del Dominio
```
barrio_caba:
  - palermo
  - recoleta
  - san_telmo
  - san_nicolas
  - puerto_madero

tipo_lugar:
  - punto_interes
  - alojamiento
  - nodo_urbano

origen_lugar:
  - curado_local
  - google_places

restriccion_dietaria:
  - celiaco
  - vegano
  - vegetariano
  - intolerante_lactosa
```

### Tablas y Entidades

#### `categorias_lugar`
* `id` (smallint, PK, autoincremental)
* `codigo` (varchar(40), unique, no nulo — ej: clave lógica compartida con core)
* `nombre` (varchar(80), no nulo)

#### `lugares`
* `id` (uuid, PK, default gen_random_uuid())
* `origen` (origen_lugar, default `curado_local`)
* `id_google_place` (varchar(100), unique, nulo si es curado)
* `fecha_ultima_sincronizacion` (timestamptz, nulo)
* `tipo_lugar` (tipo_lugar, default `punto_interes`)
* `nombre` (varchar(150), no nulo)
* `barrio` (barrio_caba, no nulo)
* `subzona` (varchar(40), nulo — ej: Soho, Hollywood, Chico)
* `direccion` (varchar(200), no nulo)
* `latitud` (decimal(10,8), no nulo)
* `longitud` (decimal(11,8), no nulo)
* `descripcion` (text, nulo)
* `duracion_estimada_minutos` (smallint, default 60)
* `nivel_precio` (smallint, default 1, check 1 a 3)
* `puntuacion` (decimal(2,1), nulo, check 1.0 a 5.0)
* `cantidad_resenas` (int, default 0)
* `es_bajo_techo` (boolean, default true)
* `es_al_aire_libre` (boolean, default false)
* `apto_ninos` (boolean, default true)
* `accesible_movilidad_reducida` (boolean, default false)
* `ofrece_menu_infantil` (boolean, default false)
* `activo` (boolean, default true)
* `fecha_creacion` (timestamptz, default now())
* `fecha_actualizacion` (timestamptz, default now())
* *Índices:* `(barrio, latitud, longitud)`, `(barrio, tipo_lugar, es_bajo_techo)`

#### `lugar_categorias` (N:M lugares <-> categorias_lugar)
* `lugar_id` (uuid, FK -> lugares.id [cascade])
* `categoria_id` (smallint, FK -> categorias_lugar.id [restrict])
* `es_principal` (boolean, default false)
* *PK compuesta:* `(lugar_id, categoria_id)`

#### `lugar_opciones_dietarias`
* `lugar_id` (uuid, FK -> lugares.id [cascade])
* `restriccion` (restriccion_dietaria)
* *PK compuesta:* `(lugar_id, restriccion)`

#### `fotos_lugar`
* `id` (uuid, PK, default gen_random_uuid())
* `lugar_id` (uuid, FK -> lugares.id [cascade])
* `url` (varchar(255), no nulo)
* `orden` (smallint, default 1)
* `es_portada` (boolean, default false)
* *Índice único:* `(lugar_id, orden)`

#### `resenas_lugar`
* `id` (uuid, PK, default gen_random_uuid())
* `lugar_id` (uuid, FK -> lugares.id [cascade])
* `origen` (origen_lugar, default `curado_local`)
* `autor_nombre` (varchar(100), nulo)
* `puntuacion` (smallint, check 1 a 5)
* `comentario` (varchar(500), nulo)
* `fecha_resena` (date, nulo)

#### `control_cuota_google`
* `fecha` (date, PK)
* `llamadas_realizadas` (int, default 0)
* `limite_diario` (int, no nulo)
* `circuito_abierto` (boolean, default false)
* `fecha_actualizacion` (timestamptz, default now())

---

## 4. Estructura y Arquitectura del Microservicio (.NET 8)

El microservicio se organiza bajo arquitectura limpia / capas desacopladas:

```
src/
├── Lugares.API/             # Controllers, Middlewares, DI, Program.cs
├── Lugares.Application/     # DTOs, Interfaces de servicios/repos, Lógica de aplicación
├── Lugares.Domain/          # Entidades, Enums, Excepciones del dominio
└── Lugares.Infrastructure/  # EF Core DbContext, Mapeos, Repositorios, Cliente Google Places
```

### Reglas de Dependencias
* `Lugares.Domain` no depende de nadie.
* `Lugares.Application` depende solo de `Lugares.Domain`.
* `Lugares.Infrastructure` depende de `Lugares.Application` y `Lugares.Domain`.
* `Lugares.API` depende de `Lugares.Application` y `Lugares.Infrastructure`.

### Pasos de Transformación del Template
1. Renombrar `Service` a `Lugares` (proyectos, carpetas, solución `.sln`, `Dockerfile`, `compose.yaml`).
2. Configurar cadena de conexión a PostgreSQL (`Database=lugares_db`).
3. Modelar Domain (Entidades y Enums).
4. Configurar Entity Framework Core en Infrastructure (`ApplicationDbContext` y configuraciones Fluent API).
5. Migración inicial (`InitialCreate`).
6. Implementar Repositorios y Servicios (Catálogo, integración Google Places, Circuit Breaker).
7. Implementar Controllers y endpoints REST.
