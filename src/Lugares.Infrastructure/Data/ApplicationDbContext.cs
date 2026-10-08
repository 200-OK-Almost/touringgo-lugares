using Microsoft.EntityFrameworkCore;

namespace Lugares.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }
    // Registrar entidades

}
