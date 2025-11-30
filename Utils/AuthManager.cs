using MauiAppIT13.Models;

namespace MauiAppIT13.Utils;

public sealed class AuthManager
{
    private User? _currentUser;

    public User? CurrentUser => _currentUser;

    public void SetAuthenticatedUser(User user)
    {
        _currentUser = user;
    }

    public void ClearAuthentication()
    {
        _currentUser = null;
    }

    public bool IsAuthenticated => _currentUser is not null;
}
