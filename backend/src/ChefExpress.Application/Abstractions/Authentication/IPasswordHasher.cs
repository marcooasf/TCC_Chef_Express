namespace ChefExpress.Application.Abstractions.Authentication;

public enum PasswordCheck
{
    Failed,
    Success,
    /// <summary>Correct password, but hashed with outdated parameters: store a fresh hash.</summary>
    SuccessRehashNeeded
}

public interface IPasswordHasher
{
    string Hash(string password);
    PasswordCheck Verify(string passwordHash, string password);
}
