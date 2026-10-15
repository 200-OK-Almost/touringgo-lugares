namespace Lugares.Domain.Entities;

public class FotoLugar
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LugarId { get; set; }
    public Lugar Lugar { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public short Orden { get; set; } = 1;
    public bool EsPortada { get; set; } = false;
}
