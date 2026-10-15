using Lugares.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lugares.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Lugar> Lugares => Set<Lugar>();
    public DbSet<CategoriaLugar> CategoriasLugar => Set<CategoriaLugar>();
    public DbSet<LugarCategoria> LugaresCategorias => Set<LugarCategoria>();
    public DbSet<LugarOpcionDietaria> LugaresOpcionesDietarias => Set<LugarOpcionDietaria>();
    public DbSet<FotoLugar> FotosLugar => Set<FotoLugar>();
    public DbSet<ResenaLugar> ResenasLugar => Set<ResenaLugar>();
    public DbSet<ControlCuotaGoogle> ControlCuotaGoogle => Set<ControlCuotaGoogle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
