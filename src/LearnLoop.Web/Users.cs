using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace LearnLoop.Web;

public sealed record User(string Name, string Email, byte[] Salt, byte[] Hash);

/// <summary>
/// In-memory users with salted PBKDF2 password hashes, and a simple lockout after repeated failures.
/// Demo data only: the seeded admin exists so the login flow can be tested.
/// </summary>
public sealed class UserStore
{
    public const int MaxFailedAttempts = 5;
    private const int Iterations = 100_000;

    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _failures = new(StringComparer.OrdinalIgnoreCase);

    public UserStore(string adminEmail, string adminPassword) => TryRegister("Admin", adminEmail, adminPassword, out _);

    public bool TryRegister(string name, string email, string password, out string? error)
    {
        error = Validate(name, email, password);
        if (error is not null) return false;

        var salt = RandomNumberGenerator.GetBytes(16);
        var user = new User(name.Trim(), email.Trim(), salt, Hash(password, salt));
        if (!_users.TryAdd(user.Email, user))
        {
            error = "An account with this email already exists.";
            return false;
        }

        return true;
    }

    public LoginResult SignIn(string email, string password)
    {
        email = email.Trim();
        if (email.Length == 0 || password.Length == 0) return LoginResult.MissingFields;
        if (_failures.GetValueOrDefault(email) >= MaxFailedAttempts) return LoginResult.LockedOut;

        if (_users.TryGetValue(email, out var user) && CryptographicOperations.FixedTimeEquals(user.Hash, Hash(password, user.Salt)))
        {
            _failures.TryRemove(email, out _);
            return new LoginResult(true, null, user);
        }

        // Count failures for unknown emails too, so the response doesn't reveal which accounts exist.
        _failures.AddOrUpdate(email, 1, (_, n) => n + 1);
        return LoginResult.InvalidCredentials;
    }

    public User? Find(string email) => _users.GetValueOrDefault(email);

    private static string? Validate(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Name is required.";
        if (!email.Contains('@') || email.Trim().Contains(' ')) return "Enter a valid email address.";
        if (password.Length < 8) return "Password must be at least 8 characters.";
        return null;
    }

    private static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
}

public sealed record LoginResult(bool Succeeded, string? Error, User? User)
{
    public static readonly LoginResult MissingFields = new(false, "Enter your email and password.", null);
    public static readonly LoginResult InvalidCredentials = new(false, "Invalid email or password.", null);
    public static readonly LoginResult LockedOut = new(false, "Too many failed attempts. Your account is locked for 15 minutes.", null);
}
