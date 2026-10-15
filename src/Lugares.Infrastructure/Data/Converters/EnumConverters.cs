using Lugares.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lugares.Infrastructure.Data.Converters;

public static class EnumConverters
{
    public static readonly ValueConverter<BarrioCaba, string> BarrioCabaConverter =
        new(
            v => v == BarrioCaba.Palermo ? "palermo" :
                 v == BarrioCaba.Recoleta ? "recoleta" :
                 v == BarrioCaba.SanTelmo ? "san_telmo" :
                 v == BarrioCaba.SanNicolas ? "san_nicolas" :
                 v == BarrioCaba.PuertoMadero ? "puerto_madero" : "palermo",
            v => v == "palermo" ? BarrioCaba.Palermo :
                 v == "recoleta" ? BarrioCaba.Recoleta :
                 v == "san_telmo" ? BarrioCaba.SanTelmo :
                 v == "san_nicolas" ? BarrioCaba.SanNicolas :
                 v == "puerto_madero" ? BarrioCaba.PuertoMadero : BarrioCaba.Palermo);

    public static readonly ValueConverter<TipoLugar, string> TipoLugarConverter =
        new(
            v => v == TipoLugar.PuntoInteres ? "punto_interes" :
                 v == TipoLugar.Alojamiento ? "alojamiento" :
                 v == TipoLugar.NodoUrbano ? "nodo_urbano" : "punto_interes",
            v => v == "punto_interes" ? TipoLugar.PuntoInteres :
                 v == "alojamiento" ? TipoLugar.Alojamiento :
                 v == "nodo_urbano" ? TipoLugar.NodoUrbano : TipoLugar.PuntoInteres);

    public static readonly ValueConverter<OrigenLugar, string> OrigenLugarConverter =
        new(
            v => v == OrigenLugar.CuradoLocal ? "curado_local" :
                 v == OrigenLugar.GooglePlaces ? "google_places" : "curado_local",
            v => v == "curado_local" ? OrigenLugar.CuradoLocal :
                 v == "google_places" ? OrigenLugar.GooglePlaces : OrigenLugar.CuradoLocal);

    public static readonly ValueConverter<RestriccionDietaria, string> RestriccionDietariaConverter =
        new(
            v => v == RestriccionDietaria.Celiaco ? "celiaco" :
                 v == RestriccionDietaria.Vegano ? "vegano" :
                 v == RestriccionDietaria.Vegetariano ? "vegetariano" :
                 v == RestriccionDietaria.IntoleranteLactosa ? "intolerante_lactosa" : "celiaco",
            v => v == "celiaco" ? RestriccionDietaria.Celiaco :
                 v == "vegano" ? RestriccionDietaria.Vegano :
                 v == "vegetariano" ? RestriccionDietaria.Vegetariano :
                 v == "intolerante_lactosa" ? RestriccionDietaria.IntoleranteLactosa : RestriccionDietaria.Celiaco);
}
