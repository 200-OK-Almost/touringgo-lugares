using Lugares.Domain.Entities;

namespace Lugares.Application.Interfaces
{
    public interface ILugarEmbeddingTextBuilder
    {
        string Build(Lugar lugar);
    }
}
