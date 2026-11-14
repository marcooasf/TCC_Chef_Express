using ChefExpress.Domain.Professionals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefExpress.Infrastructure.Persistence.Configurations;

internal sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("skills");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();

        // Initial list from the planning doc (section 8, still "[A VERIFICAR]").
        // To change it: edit here and create a new migration.
        builder.HasData(
            new Skill(1, "Marmitas Semanais"),
            new Skill(2, "Churrasco"),
            new Skill(3, "Confeitaria"),
            new Skill(4, "Jantar Executivo"),
            new Skill(5, "Comida Vegetariana/Vegana"));
    }
}
