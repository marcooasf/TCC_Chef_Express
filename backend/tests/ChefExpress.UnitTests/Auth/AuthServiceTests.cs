using ChefExpress.Application.Abstractions.Authentication;
using ChefExpress.Application.Abstractions.Persistence;
using ChefExpress.Application.Auth;
using ChefExpress.Application.Common;
using ChefExpress.Domain.Users;

namespace ChefExpress.UnitTests.Auth;

public sealed class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly AuthService _service;

    public AuthServiceTests() =>
        _service = new AuthService(_users, _unitOfWork, _hasher, new FakeTokenService());

    private static RegisterRequest ValidRegister(UserRole role = UserRole.Client) =>
        new("Maria Silva", "  Maria@Example.com ", "senhaSegura1", role);

    [Fact]
    public async Task Register_creates_user_with_hashed_password_and_returns_token()
    {
        var result = await _service.RegisterAsync(ValidRegister());

        Assert.True(result.IsSuccess);
        var user = Assert.Single(_users.Items);
        Assert.Equal("maria@example.com", user.Email);
        Assert.Equal("hashed:senhaSegura1", user.PasswordHash);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Equal($"token-for-{user.Id}", result.Value.AccessToken);
        Assert.Equal(UserRole.Client, result.Value.User.Role);
    }

    [Fact]
    public async Task Register_professional_creates_profile()
    {
        await _service.RegisterAsync(ValidRegister(UserRole.Professional));

        Assert.NotNull(Assert.Single(_users.Items).ProfessionalProfile);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email_case_insensitively()
    {
        await _service.RegisterAsync(ValidRegister());

        var result = await _service.RegisterAsync(ValidRegister() with { Email = "MARIA@example.com" });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.EmailAlreadyInUse, result.Error);
        Assert.Single(_users.Items);
    }

    [Theory]
    [InlineData(null, "maria@example.com", "senhaSegura1", UserRole.Client, "name")]
    [InlineData("Maria", "nao-e-email", "senhaSegura1", UserRole.Client, "email")]
    [InlineData("Maria", "Maria <maria@example.com>", "senhaSegura1", UserRole.Client, "email")]
    [InlineData("Maria", "maria@example.com", "curta1", UserRole.Client, "password")]
    [InlineData("Maria", "maria@example.com", "somenteletras", UserRole.Client, "password")]
    [InlineData("Maria", "maria@example.com", "senhaSegura1", null, "role")]
    [InlineData("Maria", "maria@example.com", "senhaSegura1", (UserRole)42, "role")]
    public async Task Register_validates_input(string? name, string? email, string? password, UserRole? role, string field)
    {
        var result = await _service.RegisterAsync(new RegisterRequest(name, email, password, role));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains(field, result.Error.Details!.Keys);
        Assert.Empty(_users.Items);
    }

    [Fact]
    public async Task Login_with_correct_credentials_returns_token()
    {
        await _service.RegisterAsync(ValidRegister());

        var result = await _service.LoginAsync(new LoginRequest("MARIA@example.com", "senhaSegura1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("maria@example.com", result.Value.User.Email);
    }

    [Theory]
    [InlineData("maria@example.com", "senhaErrada1")]
    [InlineData("ninguem@example.com", "senhaSegura1")]
    public async Task Login_failures_return_the_same_generic_error(string email, string password)
    {
        await _service.RegisterAsync(ValidRegister());

        var result = await _service.LoginAsync(new LoginRequest(email, password));

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Login_rehashes_outdated_password_hash()
    {
        await _service.RegisterAsync(ValidRegister());
        _hasher.NextVerifyResult = PasswordCheck.SuccessRehashNeeded;

        var result = await _service.LoginAsync(new LoginRequest("maria@example.com", "senhaSegura1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Login_requires_email_and_password()
    {
        var result = await _service.LoginAsync(new LoginRequest(" ", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(["email", "password"], result.Error.Details!.Keys.Order());
    }

    [Fact]
    public async Task Get_current_user_returns_not_found_for_unknown_id()
    {
        var result = await _service.GetCurrentUserAsync(Guid.NewGuid());

        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Items { get; } = [];

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(u => u.Email == normalizedEmail));

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.Email == normalizedEmail));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

        public void Add(User user) => Items.Add(user);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordCheck NextVerifyResult { get; set; } = PasswordCheck.Success;

        public string Hash(string password) => $"hashed:{password}";

        public PasswordCheck Verify(string passwordHash, string password) =>
            passwordHash == Hash(password) ? NextVerifyResult : PasswordCheck.Failed;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessToken CreateAccessToken(User user) => new($"token-for-{user.Id}", DateTime.UtcNow.AddHours(1));
    }
}
