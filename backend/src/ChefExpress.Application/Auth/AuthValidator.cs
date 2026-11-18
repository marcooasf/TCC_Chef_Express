using System.Net.Mail;
using ChefExpress.Domain.Users;

namespace ChefExpress.Application.Auth;

/// <summary>Input validation for sign-up/login. Keys match the JSON field names (camelCase).</summary>
internal static class AuthValidator
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public static Dictionary<string, string[]> Validate(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            errors["name"] = ["Informe o nome."];
        else if (name.Length > User.NameMaxLength)
            errors["name"] = [$"O nome deve ter no máximo {User.NameMaxLength} caracteres."];

        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(email))
            errors["email"] = ["Informe o e-mail."];
        else if (email.Length > User.EmailMaxLength)
            errors["email"] = [$"O e-mail deve ter no máximo {User.EmailMaxLength} caracteres."];
        else if (!IsValidEmail(email))
            errors["email"] = ["Informe um e-mail válido."];

        var password = request.Password ?? string.Empty;
        var passwordErrors = new List<string>();
        if (password.Length < PasswordMinLength)
            passwordErrors.Add($"A senha deve ter pelo menos {PasswordMinLength} caracteres.");
        if (password.Length > PasswordMaxLength)
            passwordErrors.Add($"A senha deve ter no máximo {PasswordMaxLength} caracteres.");
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            passwordErrors.Add("A senha deve conter letras e números.");
        if (passwordErrors.Count > 0)
            errors["password"] = [.. passwordErrors];

        if (request.Role is null || !Enum.IsDefined(request.Role.Value))
            errors["role"] = ["Escolha o perfil: Client (Cliente) ou Professional (Profissional)."];

        return errors;
    }

    public static Dictionary<string, string[]> Validate(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = ["Informe o e-mail."];
        if (string.IsNullOrEmpty(request.Password))
            errors["password"] = ["Informe a senha."];
        return errors;
    }

    private static bool IsValidEmail(string value) =>
        MailAddress.TryCreate(value, out var address)
        && address.Address == value          // rejects display-name forms like "Ana <a@b.com>"
        && address.Host.Contains('.');
}
