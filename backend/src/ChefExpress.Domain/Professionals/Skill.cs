namespace ChefExpress.Domain.Professionals;

/// <summary>Gastronomic specialty (e.g. "Churrasco", "Confeitaria"). Fixed list, seeded by migrations.</summary>
public sealed class Skill
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;

    public ICollection<ProfessionalProfile> Professionals { get; private set; } = [];

    private Skill() { } // EF Core

    public Skill(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
