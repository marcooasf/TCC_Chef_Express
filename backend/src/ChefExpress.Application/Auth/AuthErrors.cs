using ChefExpress.Application.Common;

namespace ChefExpress.Application.Auth;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyInUse =
        new("Auth.EmailAlreadyInUse", "Este e-mail já está cadastrado.", ErrorType.Conflict);

    /// <summary>Same message for unknown e-mail and wrong password, so accounts cannot be enumerated.</summary>
    public static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "E-mail ou senha inválidos.", ErrorType.Unauthorized);

    public static readonly Error UserNotFound =
        new("Auth.UserNotFound", "Usuário não encontrado.", ErrorType.NotFound);
}
