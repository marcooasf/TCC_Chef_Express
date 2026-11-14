using ChefExpress.Domain.Professionals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefExpress.Infrastructure.Persistence.Configurations;

internal sealed class AvailableDateConfiguration : IEntityTypeConfiguration<AvailableDate>
{
    public void Configure(EntityTypeBuilder<AvailableDate> builder)
    {
        builder.ToTable("available_dates");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Date).HasColumnType("date").IsRequired();
        builder.Property(d => d.IsBooked).HasDefaultValue(false);

        // One row per professional per day
        builder.HasIndex(d => new { d.ProfessionalProfileId, d.Date }).IsUnique();
        // Search: "who is free on date X?"
        builder.HasIndex(d => new { d.Date, d.IsBooked });
    }
}
