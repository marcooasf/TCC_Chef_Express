using ChefExpress.Application.Abstractions.Persistence;
using ChefExpress.Domain.Professionals;
using ChefExpress.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ChefExpress.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ProfessionalProfile> ProfessionalProfiles => Set<ProfessionalProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<AvailableDate> AvailableDates => Set<AvailableDate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCharSet("utf8mb4");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
