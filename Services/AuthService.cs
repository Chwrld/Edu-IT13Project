using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Services;

public sealed class AuthService
{
    private readonly DbConnection _dbConnection;
    private readonly PasswordHasher _passwordHasher;
    private readonly ActivityLogger _activityLogger;

    public AuthService(DbConnection dbConnection, PasswordHasher passwordHasher, ActivityLogger activityLogger)
    {
        _dbConnection = dbConnection;
        _passwordHasher = passwordHasher;
        _activityLogger = activityLogger;
    }

    public async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Failed("Email and password are required.");
        }

        var user = await _dbConnection.GetUserByEmailAsync(email);
        if (user is null)
        {
            _activityLogger.LogWarning($"Authentication failed. User not found for email: {email}");
            return AuthResult.Failed("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _activityLogger.LogWarning($"Authentication blocked for inactive user: {email}");
            return AuthResult.Failed("Your account is currently inactive. Please contact support.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
        if (!isPasswordValid)
        {
            _activityLogger.LogWarning($"Authentication failed. Incorrect password for email: {email}");
            return AuthResult.Failed("Invalid email or password.");
        }

        _activityLogger.LogInfo($"User authenticated successfully: {email}");
        return AuthResult.Succeeded(user);
    }
}
