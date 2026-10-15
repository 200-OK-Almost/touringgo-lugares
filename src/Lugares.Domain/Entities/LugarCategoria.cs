namespace Lugares.Domain.Entities;

public class LugarCategoria
{
    public Guid LugarId { get; set; }
    public Lugar Lugar { get; set; } = null!;

    public short CategoriaId { get; set; }
    public CategoriaLugar Categoria { get; set; } = null!;

    public bool EsPrincipal { get; set; } = false;
}
