using Microsoft.EntityFrameworkCore;

namespace IdentityService;

public class AuthDbContext : DbContext
{
    public AuthDbContext()
    {

    }
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
