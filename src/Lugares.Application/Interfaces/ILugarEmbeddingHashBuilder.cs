using Lugares.Domain.Entities;

namespace Lugares.Application.Interfaces;

public interface ILugarEmbeddingHashBuilder
{
    string Build(Lugar lugar);
}
