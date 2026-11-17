using ChefExpress.Domain.Professionals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefExpress.Infrastructure.Persistence.Configurations;

internal sealed class ProfessionalProfileConfiguration : IEntityTypeConfiguration<ProfessionalProfile>
{
    public void Configure(EntityTypeBuilder<ProfessionalProfile> builder)
    {
        builder.ToTable("professional_profiles");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.UserId).IsUnique();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.City).HasMaxLength(100).IsRequired();
        builder.Property(p => p.State).HasMaxLength(2).IsRequired();
        builder.Property(p => p.Neighborhood).HasMaxLength(100);
        builder.Property(p => p.BasePrice).HasPrecision(10, 2);
        builder.Property(p => p.UpdatedAt).IsRequired();

        // Indexes used by the search with filters (task 9)
        builder.HasIndex(p => new { p.State, p.City });
        builder.HasIndex(p => p.BasePrice);

        // Proximity search: bounding-box pre-filter (see ProximityQueryExtensions)
        builder.Ignore(p => p.Location);
        builder.HasIndex(p => new { p.Latitude, p.Longitude });

        builder.HasMany(p => p.Skills)
            .WithMany(s => s.Professionals)
            .UsingEntity(
                "professional_skills",
                r => r.HasOne(typeof(Skill)).WithMany().HasForeignKey("SkillId").OnDelete(DeleteBehavior.Cascade),
                l => l.HasOne(typeof(ProfessionalProfile)).WithMany().HasForeignKey("ProfessionalProfileId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey("ProfessionalProfileId", "SkillId"));

        builder.HasMany(p => p.AvailableDates)
            .WithOne()
            .HasForeignKey(d => d.ProfessionalProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
