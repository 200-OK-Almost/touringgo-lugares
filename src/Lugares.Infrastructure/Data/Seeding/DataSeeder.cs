using Lugares.Domain.Entities;
using Lugares.Domain.Enums;
using Lugares.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lugares.Infrastructure.Data.Seeding;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 1. Seed de Categorías de Lugar (Catálogo de 7 categorías oficiales del DER)
        if (!await context.CategoriasLugar.AnyAsync())
        {
            var categorias = new List<CategoriaLugar>
            {
                new() { Codigo = "cafe_especialidad", Nombre = "Café de especialidad" },
                new() { Codigo = "museos_galerias", Nombre = "Museos y galerías" },
                new() { Codigo = "parques_aire_libre", Nombre = "Parques y aire libre" },
                new() { Codigo = "librerias_diseno", Nombre = "Librerías y diseño" },
                new() { Codigo = "bares_vermu", Nombre = "Bares y vermú" },
                new() { Codigo = "gastronomia_clasica", Nombre = "Gastronomía clásica/Bodegón" },
                new() { Codigo = "cocteleria_autor", Nombre = "Coctelería de autor" }
            };

            await context.CategoriasLugar.AddRangeAsync(categorias);
            await context.SaveChangesAsync();
        }

        // 2. Seed de Lugares Curados de CABA (35 lugares distribuidos en los 5 barrios)
        if (!await context.Lugares.AnyAsync())
        {
            var catCafe = await context.CategoriasLugar.FirstAsync(c => c.Codigo == "cafe_especialidad");
            var catMuseos = await context.CategoriasLugar.FirstAsync(c => c.Codigo == "museos_galerias");
            var catParques = await context.CategoriasLugar.FirstAsync(c => c.Codigo == "parques_aire_libre");
            var catLibrerias = await context.CategoriasLugar.FirstAsync(c => c.Codigo == "librerias_diseno");
            var catBares = await context.CategoriasLugar.FirstAsync(c => c.Codigo == "bares_vermu");
            var catBodegon = await context.CategoriasLugar.FirstAsync(c => c.Codigo == "gastronomia_clasica");
            var catCocteleria = await context.CategoriasLugar.FirstAsync(c => c.Codigo == "cocteleria_autor");

            var lugares = new List<Lugar>
            {
                // ==================== PALERMO ====================
                new()
                {
                    Nombre = "Café Lattente",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Palermo,
                    Subzona = "Soho",
                    Direccion = "Thames 1891",
                    Latitud = -34.58850000m,
                    Longitud = -58.42820000m,
                    Descripcion = "Pioneros del café de especialidad en Buenos Aires, tostado propio y mesas en la vereda.",
                    DuracionEstimadaMinutos = 45,
                    NivelPrecio = 1,
                    Puntuacion = 4.7m,
                    CantidadResenas = 1250,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    OfreceMenuInfantil = false,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catCafe.Id, EsPrincipal = true } },
                    OpcionesDietarias = new List<LugarOpcionDietaria> { new() { Restriccion = RestriccionDietaria.Vegano } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Libros del Pasaje",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Palermo,
                    Subzona = "Soho",
                    Direccion = "Thames 1762",
                    Latitud = -34.58880000m,
                    Longitud = -58.42590000m,
                    Descripcion = "Librería encantadora con patio interno, sillones para leer y café literario.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 2,
                    Puntuacion = 4.8m,
                    CantidadResenas = 980,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria>
                    {
                        new() { CategoriaId = catLibrerias.Id, EsPrincipal = true },
                        new() { CategoriaId = catCafe.Id, EsPrincipal = false }
                    },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1521587760476-6c12a4b040da", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Don Julio",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Palermo,
                    Subzona = "Soho",
                    Direccion = "Guatemala 4699",
                    Latitud = -34.58890000m,
                    Longitud = -58.42390000m,
                    Descripcion = "Reconocida parrilla tradicional argentina, carnes de pastura y extensa cava de vinos.",
                    DuracionEstimadaMinutos = 90,
                    NivelPrecio = 3,
                    Puntuacion = 4.9m,
                    CantidadResenas = 4200,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    OfreceMenuInfantil = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catBodegon.Id, EsPrincipal = true } },
                    OpcionesDietarias = new List<LugarOpcionDietaria> { new() { Restriccion = RestriccionDietaria.Celiaco } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1544025162-d76694265947", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Tres Monos Bar",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Palermo,
                    Subzona = "Soho",
                    Direccion = "Guatemala 4899",
                    Latitud = -34.58720000m,
                    Longitud = -58.42440000m,
                    Descripcion = "Bar de coctelería premiado internacionalmente con ambiente relajado y tragos de autor.",
                    DuracionEstimadaMinutos = 75,
                    NivelPrecio = 2,
                    Puntuacion = 4.8m,
                    CantidadResenas = 1100,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = false,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catCocteleria.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Rosedal de Palermo (Parque 3 de Febrero)",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Palermo,
                    Subzona = "Chico",
                    Direccion = "Av. Infanta Isabel 900",
                    Latitud = -34.57140000m,
                    Longitud = -58.41720000m,
                    Descripcion = "Parque histórico con más de 18.000 rosales, lago con botes y puente griego.",
                    DuracionEstimadaMinutos = 90,
                    NivelPrecio = 1,
                    Puntuacion = 4.9m,
                    CantidadResenas = 5300,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1588714477688-cf28a50e94f7", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Jardín Botánico Carlos Thays",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Palermo,
                    Subzona = "Botánico",
                    Direccion = "Av. Santa Fe 3951",
                    Latitud = -34.58240000m,
                    Longitud = -58.41840000m,
                    Descripcion = "Pulmón verde con colecciones botánicas, senderos escultóricos e invernáculo art nouveau.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 1,
                    Puntuacion = 4.8m,
                    CantidadResenas = 3100,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1519331379826-f10be5486c6f", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "MALBA (Museo de Arte Latinoamericano)",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Palermo,
                    Subzona = "Chico",
                    Direccion = "Av. Figueroa Alcorta 3415",
                    Latitud = -34.57700000m,
                    Longitud = -58.40320000m,
                    Descripcion = "Colección referente de arte moderno y contemporáneo latinoamericano con exhibiciones temporales.",
                    DuracionEstimadaMinutos = 90,
                    NivelPrecio = 2,
                    Puntuacion = 4.8m,
                    CantidadResenas = 3400,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catMuseos.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1565008447742-97f6f38c985c", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "La Fuerza Bar Vermú",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Palermo,
                    Subzona = "Hollywood",
                    Direccion = "Av. Dorrego 1409",
                    Latitud = -34.58770000m,
                    Longitud = -58.44110000m,
                    Descripcion = "Vermutería moderna con producción propia elaborada al pie de los Andes y platitos porteños.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 2,
                    Puntuacion = 4.6m,
                    CantidadResenas = 820,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = false,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catBares.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1510812431401-41d2bd2722f3", Orden = 1, EsPortada = true }
                    }
                },

                // ==================== RECOLETA ====================
                new()
                {
                    Nombre = "El Ateneo Grand Splendid",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Recoleta,
                    Direccion = "Av. Santa Fe 1860",
                    Latitud = -34.59590000m,
                    Longitud = -58.39230000m,
                    Descripcion = "Famosa librería instalada en un antiguo teatro de 1919, con cúpula pintada y café en el escenario.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 2,
                    Puntuacion = 4.9m,
                    CantidadResenas = 8900,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria>
                    {
                        new() { CategoriaId = catLibrerias.Id, EsPrincipal = true },
                        new() { CategoriaId = catCafe.Id, EsPrincipal = false }
                    },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1507842229451-7f01be7fe88d", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Museo Nacional de Bellas Artes",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Recoleta,
                    Direccion = "Av. del Libertador 1473",
                    Latitud = -34.58390000m,
                    Longitud = -58.39300000m,
                    Descripcion = "El museo de arte más importante de Argentina con obras de Goya, Van Gogh, Rodin y artistas nacionales.",
                    DuracionEstimadaMinutos = 100,
                    NivelPrecio = 1,
                    Puntuacion = 4.8m,
                    CantidadResenas = 4600,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catMuseos.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Cementerio de la Recoleta",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Recoleta,
                    Direccion = "Junín 1760",
                    Latitud = -34.58790000m,
                    Longitud = -58.39270000m,
                    Descripcion = "Monumento histórico con impactante arquitectura funeraria y mausoleos de próceres y figuras como Eva Perón.",
                    DuracionEstimadaMinutos = 75,
                    NivelPrecio = 2,
                    Puntuacion = 4.7m,
                    CantidadResenas = 6100,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catMuseos.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1548625361-195feee819f7", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Café La Biela",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Recoleta,
                    Direccion = "Av. Quintana 600",
                    Latitud = -34.58710000m,
                    Longitud = -58.38970000m,
                    Descripcion = "Bar notable tradicional de Recoleta, punto de encuentro histórico junto a la sombra del gomero bicentenario.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 2,
                    Puntuacion = 4.5m,
                    CantidadResenas = 2300,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria>
                    {
                        new() { CategoriaId = catBodegon.Id, EsPrincipal = true },
                        new() { CategoriaId = catCafe.Id, EsPrincipal = false }
                    },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1554118811-1e0d58224f24", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Plaza Francia",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Recoleta,
                    Direccion = "Av. del Libertador y Av. Pueyrredón",
                    Latitud = -34.58550000m,
                    Longitud = -58.39120000m,
                    Descripcion = "Gran espacio verde con desniveles, ferias de artesanos los fines de semana y vista a los museos.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 1,
                    Puntuacion = 4.8m,
                    CantidadResenas = 2900,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1496868834840-5f4c98840aaa", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Floralis Genérica (Plaza de las Naciones Unidas)",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Recoleta,
                    Direccion = "Av. Pres. Figueroa Alcorta 2301",
                    Latitud = -34.58180000m,
                    Longitud = -58.39360000m,
                    Descripcion = "Monumento escultórico metálico de una flor gigante que abre y cierra sus pétalos con la luz solar.",
                    DuracionEstimadaMinutos = 45,
                    NivelPrecio = 1,
                    Puntuacion = 4.7m,
                    CantidadResenas = 2400,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1589802829985-817e51171b92", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Centro Cultural Recoleta",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.Recoleta,
                    Direccion = "Junín 1930",
                    Latitud = -34.58660000m,
                    Longitud = -58.39190000m,
                    Descripcion = "Espacio multidisciplinario con salas de arte visual, música en vivo, talleres y terrazas recreativas.",
                    DuracionEstimadaMinutos = 75,
                    NivelPrecio = 1,
                    Puntuacion = 4.7m,
                    CantidadResenas = 2200,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria>
                    {
                        new() { CategoriaId = catMuseos.Id, EsPrincipal = true },
                        new() { CategoriaId = catLibrerias.Id, EsPrincipal = false }
                    },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1499856871958-5b9627545d1a", Orden = 1, EsPortada = true }
                    }
                },

                // ==================== SAN TELMO ====================
                new()
                {
                    Nombre = "Mercado de San Telmo",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanTelmo,
                    Direccion = "Bolívar 970",
                    Latitud = -34.61920000m,
                    Longitud = -58.37340000m,
                    Descripcion = "Mercado histórico cubierto inaugurado en 1897, combinando anticuarios, puestos de café y gastronomía variada.",
                    DuracionEstimadaMinutos = 80,
                    NivelPrecio = 2,
                    Puntuacion = 4.7m,
                    CantidadResenas = 7800,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria>
                    {
                        new() { CategoriaId = catBodegon.Id, EsPrincipal = true },
                        new() { CategoriaId = catCafe.Id, EsPrincipal = false }
                    },
                    OpcionesDietarias = new List<LugarOpcionDietaria>
                    {
                        new() { Restriccion = RestriccionDietaria.Vegetariano },
                        new() { Restriccion = RestriccionDietaria.Celiaco }
                    },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Bar Plaza Dorrego",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanTelmo,
                    Direccion = "Defensa 1098",
                    Latitud = -34.62080000m,
                    Longitud = -58.37250000m,
                    Descripcion = "Bar notable conservado con sus mesas de madera talladas, fotos históricas y ambiente tanguero.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 2,
                    Puntuacion = 4.6m,
                    CantidadResenas = 1750,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria>
                    {
                        new() { CategoriaId = catBares.Id, EsPrincipal = true },
                        new() { CategoriaId = catBodegon.Id, EsPrincipal = false }
                    },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Plaza Dorrego",
                    TipoLugar = TipoLugar.NodoUrbano,
                    Barrio = BarrioCaba.SanTelmo,
                    Direccion = "Humberto 1º y Defensa",
                    Latitud = -34.62070000m,
                    Longitud = -58.37210000m,
                    Descripcion = "Corazón histórico de San Telmo, rodeada de bares coloniales y sede de la feria de antigüedades.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 1,
                    Puntuacion = 4.7m,
                    CantidadResenas = 3400,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1513836279014-a89f7a76ae86", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Museo de Arte Moderno de Buenos Aires (MAMBA)",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanTelmo,
                    Direccion = "Av. San Juan 350",
                    Latitud = -34.62250000m,
                    Longitud = -58.37050000m,
                    Descripcion = "Ubicado en un antiguo edificio tabacalero reciclado, expone arte argentino contemporáneo e innovador.",
                    DuracionEstimadaMinutos = 80,
                    NivelPrecio = 1,
                    Puntuacion = 4.7m,
                    CantidadResenas = 1600,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catMuseos.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1518998053901-5348d3961a04", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "El Banco Rojo",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanTelmo,
                    Direccion = "Bolívar 866",
                    Latitud = -34.61860000m,
                    Longitud = -58.37290000m,
                    Descripcion = "Comida callejera gourmet, sándwiches artesanales y cerveza tirada en una calle adoquinada.",
                    DuracionEstimadaMinutos = 45,
                    NivelPrecio = 1,
                    Puntuacion = 4.6m,
                    CantidadResenas = 1350,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catBares.Id, EsPrincipal = true } },
                    OpcionesDietarias = new List<LugarOpcionDietaria> { new() { Restriccion = RestriccionDietaria.Vegano } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1550547660-d9450f859349", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Parque Lezama",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanTelmo,
                    Direccion = "Brasil y Defensa",
                    Latitud = -34.62750000m,
                    Longitud = -58.36950000m,
                    Descripcion = "Parque tradicional con barranca arbolada, anfiteatro, glorietas y vista al Museo Histórico Nacional.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 1,
                    Puntuacion = 4.7m,
                    CantidadResenas = 3100,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1448375240586-882707db888b", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Pasaje San Lorenzo y Casa Mínima",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanTelmo,
                    Direccion = "Pasaje San Lorenzo 380",
                    Latitud = -34.61790000m,
                    Longitud = -58.37080000m,
                    Descripcion = "El frente más angosto de la ciudad (2,50 metros), cargado de leyendas sobre la época colonial.",
                    DuracionEstimadaMinutos = 30,
                    NivelPrecio = 1,
                    Puntuacion = 4.5m,
                    CantidadResenas = 940,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catLibrerias.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1486406146926-c627a92ad1ab", Orden = 1, EsPortada = true }
                    }
                },

                // ==================== SAN NICOLÁS ====================
                new()
                {
                    Nombre = "Obelisco de Buenos Aires",
                    TipoLugar = TipoLugar.NodoUrbano,
                    Barrio = BarrioCaba.SanNicolas,
                    Direccion = "Av. 9 de Julio y Corrientes",
                    Latitud = -34.60370000m,
                    Longitud = -58.38160000m,
                    Descripcion = "Monumento ícono absoluto de la Ciudad de Buenos Aires en la intersección de dos de sus avenidas principales.",
                    DuracionEstimadaMinutos = 30,
                    NivelPrecio = 1,
                    Puntuacion = 4.8m,
                    CantidadResenas = 12000,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1589802829985-817e51171b92", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Teatro Colón",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanNicolas,
                    Direccion = "Cerrito 628",
                    Latitud = -34.60110000m,
                    Longitud = -58.38310000m,
                    Descripcion = "Uno de los teatros de ópera con mejor acústica del mundo, con visitas guiadas por sus salones dorados.",
                    DuracionEstimadaMinutos = 90,
                    NivelPrecio = 3,
                    Puntuacion = 4.9m,
                    CantidadResenas = 9500,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catMuseos.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1516450360452-9312f5e86fc7", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Café Tortoni",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanNicolas,
                    Direccion = "Av. de Mayo 825",
                    Latitud = -34.60840000m,
                    Longitud = -58.37920000m,
                    Descripcion = "El café más antiguo y famoso de Buenos Aires (1858), sede histórica de tertulias de Borges, Gardel y Alfonsina Storni.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 2,
                    Puntuacion = 4.7m,
                    CantidadResenas = 11000,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria>
                    {
                        new() { CategoriaId = catCafe.Id, EsPrincipal = true },
                        new() { CategoriaId = catBodegon.Id, EsPrincipal = false }
                    },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Pizzería Güerrin",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanNicolas,
                    Direccion = "Av. Corrientes 1368",
                    Latitud = -34.60430000m,
                    Longitud = -58.38600000m,
                    Descripcion = "Templo de la pizza al molde porteña y faina sobre la Avenida Corrientes desde 1932.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 2,
                    Puntuacion = 4.8m,
                    CantidadResenas = 15000,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    OfreceMenuInfantil = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catBodegon.Id, EsPrincipal = true } },
                    OpcionesDietarias = new List<LugarOpcionDietaria> { new() { Restriccion = RestriccionDietaria.Vegetariano } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1513104890138-7c749659a591", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Los Galgos Bar",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanNicolas,
                    Direccion = "Av. Callao 501",
                    Latitud = -34.60530000m,
                    Longitud = -58.39210000m,
                    Descripcion = "Esquina clásica restaurada con café de especialidad, vermut de grifo y gastronomía porteña refinada.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 2,
                    Puntuacion = 4.6m,
                    CantidadResenas = 2100,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria>
                    {
                        new() { CategoriaId = catBares.Id, EsPrincipal = true },
                        new() { CategoriaId = catCafe.Id, EsPrincipal = false }
                    },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1559925393-8be0ec4767c8", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Plaza Lavalle",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanNicolas,
                    Direccion = "Talcahuano y Lavalle",
                    Latitud = -34.60190000m,
                    Longitud = -58.38480000m,
                    Descripcion = "Plaza de tres manzanas frente al Teatro Colón y el Palacio de Justicia con árboles centenarios.",
                    DuracionEstimadaMinutos = 45,
                    NivelPrecio = 1,
                    Puntuacion = 4.6m,
                    CantidadResenas = 1800,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1496868834840-5f4c98840aaa", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Librería de Ávila",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.SanNicolas,
                    Direccion = "Alsina 500",
                    Latitud = -34.61090000m,
                    Longitud = -58.37420000m,
                    Descripcion = "La librería más antigua de Buenos Aires, especializada en historia argentina, libros descatalogados y ensayos.",
                    DuracionEstimadaMinutos = 45,
                    NivelPrecio = 1,
                    Puntuacion = 4.8m,
                    CantidadResenas = 850,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catLibrerias.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1457369804613-52c61a468e7d", Orden = 1, EsPortada = true }
                    }
                },

                // ==================== PUERTO MADERO ====================
                new()
                {
                    Nombre = "Puente de la Mujer",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.PuertoMadero,
                    Direccion = "Dique 3, Puerto Madero",
                    Latitud = -34.60780000m,
                    Longitud = -58.36440000m,
                    Descripcion = "Puente peatonal giratorio diseñado por Santiago Calatrava, evocando una pareja bailando tango.",
                    DuracionEstimadaMinutos = 40,
                    NivelPrecio = 1,
                    Puntuacion = 4.8m,
                    CantidadResenas = 9200,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1589802829985-817e51171b92", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Reserva Ecológica Costanera Sur",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.PuertoMadero,
                    Direccion = "Av. Tristán Achával Rodríguez 1550",
                    Latitud = -34.61710000m,
                    Longitud = -58.35120000m,
                    Descripcion = "Espacio verde natural de 350 hectáreas junto al Río de la Plata con lagunas, bosques y senderos.",
                    DuracionEstimadaMinutos = 120,
                    NivelPrecio = 1,
                    Puntuacion = 4.8m,
                    CantidadResenas = 6700,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1448375240586-882707db888b", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Cabaña Las Lilas",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.PuertoMadero,
                    Direccion = "Av. Alicia Moreau de Justo 516",
                    Latitud = -34.60670000m,
                    Longitud = -58.36620000m,
                    Descripcion = "Alta cocina de carnes y bodega premium con vista panorámica a los diques de Puerto Madero.",
                    DuracionEstimadaMinutos = 90,
                    NivelPrecio = 3,
                    Puntuacion = 4.7m,
                    CantidadResenas = 3800,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    OfreceMenuInfantil = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catBodegon.Id, EsPrincipal = true } },
                    OpcionesDietarias = new List<LugarOpcionDietaria> { new() { Restriccion = RestriccionDietaria.Celiaco } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1544025162-d76694265947", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Colección de Arte Amalia Lacroze de Fortabat",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.PuertoMadero,
                    Direccion = "Olga Cossettini 141",
                    Latitud = -34.60090000m,
                    Longitud = -58.36290000m,
                    Descripcion = "Museo de arte en un edificio contemporáneo frente al Dique 4 con obras de arte internacional y argentino.",
                    DuracionEstimadaMinutos = 75,
                    NivelPrecio = 2,
                    Puntuacion = 4.7m,
                    CantidadResenas = 1500,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catMuseos.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1565008447742-97f6f38c985c", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Library Lounge (Faena Hotel)",
                    TipoLugar = TipoLugar.Alojamiento,
                    Barrio = BarrioCaba.PuertoMadero,
                    Direccion = "Martha Salotti 445",
                    Latitud = -34.61330000m,
                    Longitud = -58.36250000m,
                    Descripcion = "Bar de coctelería íntimo con diseño de Philippe Starck, sillones de terciopelo y música en vivo.",
                    DuracionEstimadaMinutos = 80,
                    NivelPrecio = 3,
                    Puntuacion = 4.8m,
                    CantidadResenas = 1100,
                    EsBajoTecho = true,
                    EsAlAireLibre = true,
                    AptoNinos = false,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catCocteleria.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b", Orden = 1, EsPortada = true }
                    }
                },
                new()
                {
                    Nombre = "Parque Mujeres Argentinas",
                    TipoLugar = TipoLugar.PuntoInteres,
                    Barrio = BarrioCaba.PuertoMadero,
                    Direccion = "Juana Manso y Manuela Sáenz",
                    Latitud = -34.60720000m,
                    Longitud = -58.36100000m,
                    Descripcion = "Parque central amplio con colinas de césped, senderos para caminata y vista a los rascacielos.",
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = 1,
                    Puntuacion = 4.6m,
                    CantidadResenas = 2100,
                    EsBajoTecho = false,
                    EsAlAireLibre = true,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = true,
                    Categorias = new List<LugarCategoria> { new() { CategoriaId = catParques.Id, EsPrincipal = true } },
                    Fotos = new List<FotoLugar>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1496868834840-5f4c98840aaa", Orden = 1, EsPortada = true }
                    }
                }
            };

            await context.Lugares.AddRangeAsync(lugares);
            await context.SaveChangesAsync();
        }
    }
}
