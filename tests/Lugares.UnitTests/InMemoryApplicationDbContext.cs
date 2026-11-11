using Lugares.Domain.Entities;
using Lugares.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lugares.UnitTests;

internal sealed class InMemoryApplicationDbContext : ApplicationDbContext
{
    public InMemoryApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // El proveedor InMemory no soporta tipos espaciales (postgis) ni de vectores (pgvector).
        modelBuilder.Entity<Lugar>().Ignore(l => l.Ubicacion);
        modelBuilder.Entity<Lugar>().Ignore(l => l.Embedding);
    }
}
