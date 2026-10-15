using Lugares.Domain.Enums;

namespace Lugares.Domain.Entities;

public class LugarOpcionDietaria
{
    public Guid LugarId { get; set; }
    public Lugar Lugar { get; set; } = null!;

    public RestriccionDietaria Restriccion { get; set; }
}
