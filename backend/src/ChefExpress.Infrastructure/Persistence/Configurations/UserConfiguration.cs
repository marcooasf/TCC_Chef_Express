using ChefExpress.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefExpress.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).HasMaxLength(User.NameMaxLength).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasOne(u => u.ProfessionalProfile)
            .WithOne(p => p.User)
            .HasForeignKey<Domain.Professionals.ProfessionalProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
