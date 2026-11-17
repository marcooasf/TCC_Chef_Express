namespace ChefExpress.Domain.Geography;

/// <summary>The 27 federative units (UF) of Brazil.</summary>
public static class BrazilianStates
{
    private static readonly Dictionary<string, string> NamesByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AC"] = "Acre",
        ["AL"] = "Alagoas",
        ["AP"] = "Amapá",
        ["AM"] = "Amazonas",
        ["BA"] = "Bahia",
        ["CE"] = "Ceará",
        ["DF"] = "Distrito Federal",
        ["ES"] = "Espírito Santo",
        ["GO"] = "Goiás",
        ["MA"] = "Maranhão",
        ["MT"] = "Mato Grosso",
        ["MS"] = "Mato Grosso do Sul",
        ["MG"] = "Minas Gerais",
        ["PA"] = "Pará",
        ["PB"] = "Paraíba",
        ["PR"] = "Paraná",
        ["PE"] = "Pernambuco",
        ["PI"] = "Piauí",
        ["RJ"] = "Rio de Janeiro",
        ["RN"] = "Rio Grande do Norte",
        ["RS"] = "Rio Grande do Sul",
        ["RO"] = "Rondônia",
        ["RR"] = "Roraima",
        ["SC"] = "Santa Catarina",
        ["SP"] = "São Paulo",
        ["SE"] = "Sergipe",
        ["TO"] = "Tocantins"
    };

    public static bool IsValid(string? code) => code is not null && NamesByCode.ContainsKey(code.Trim());

    /// <summary>Full name for a UF code (e.g. "SC" → "Santa Catarina"), or null if unknown.</summary>
    public static string? GetName(string? code) =>
        code is not null && NamesByCode.TryGetValue(code.Trim(), out var name) ? name : null;
}
