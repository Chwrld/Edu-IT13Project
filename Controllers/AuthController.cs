using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Controllers;

public sealed class AuthController
{
    private readonly ValidationHelper _validationHelper;
    private readonly AuthService _authService;
    private readonly AuthManager _authManager;
    private readonly ActivityLogger _activityLogger;

    public AuthController(
        ValidationHelper validationHelper,
        AuthService authService,
        AuthManager authManager,
        ActivityLogger activityLogger)
    {
        _validationHelper = validationHelper;
        _authService = authService;
        _authManager = authManager;
        _activityLogger = activityLogger;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var validation = _validationHelper.ValidateCredentials(email, password);
        if (!validation.IsValid)
        {
            return AuthResult.Failed(validation.ErrorMessage ?? "Invalid credentials.");
        }

        var result = await _authService.AuthenticateAsync(email, password);
        if (result.Success && result.User is not null)
        {
            _authManager.SetAuthenticatedUser(result.User);
        }
        else if (!result.Success)
        {
            _activityLogger.LogWarning($"Login attempt failed for {email}: {result.ErrorMessage}");
        }

        return result;
    }

    public void Logout() => _authManager.ClearAuthentication();

    public User? GetCurrentUser() => _authManager.CurrentUser;
}
