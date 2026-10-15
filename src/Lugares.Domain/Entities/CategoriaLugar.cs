namespace Lugares.Domain.Entities;

public class CategoriaLugar
{
    public short Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    // Propiedades de navegación
    public ICollection<LugarCategoria> LugaresCategorias { get; set; } = new List<LugarCategoria>();
}
