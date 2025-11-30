using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Controllers;

public sealed class UserController
{
    private readonly DbConnection _dbConnection;
    private readonly PasswordHasher _passwordHasher;

    public UserController(DbConnection dbConnection, PasswordHasher passwordHasher)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    /// <summary>
    /// Retrieves all users from the database.
    /// </summary>
    public async Task<IReadOnlyCollection<User>> GetAllUsersAsync()
    {
        try
        {
            return await _dbConnection.GetUsersAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve users from database.", ex);
        }
    }

    /// <summary>
    /// Searches users by display name or email.
    /// </summary>
    public async Task<IReadOnlyCollection<User>> SearchUsersAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return await GetAllUsersAsync();
        }

        var allUsers = await GetAllUsersAsync();
        var searchLower = searchText.ToLower();

        return allUsers
            .Where(u => u.DisplayName.ToLower().Contains(searchLower) ||
                       u.Email.ToLower().Contains(searchLower))
            .ToList();
    }

    /// <summary>
    /// Creates a new user with validation.
    /// </summary>
    public async Task<(bool Success, string Message, User? User)> CreateUserAsync(
        string displayName,
        string email,
        string password,
        Role role,
        string? phoneNumber = null,
        string? address = null)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(displayName))
            return (false, "Please enter a full name.", null);

        if (string.IsNullOrWhiteSpace(email))
            return (false, "Please enter an email address.", null);

        if (string.IsNullOrWhiteSpace(password))
            return (false, "Please enter a password.", null);

        if (password.Length < 6)
            return (false, "Password must be at least 6 characters long.", null);

        try
        {
            // Check if email already exists
            var existingUser = await _dbConnection.GetUserByEmailAsync(email);
            if (existingUser != null)
                return (false, "A user with this email already exists.", null);

            // Hash password
            var (hash, salt) = _passwordHasher.HashPassword(password);

            // Create new user
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = displayName,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = role,
                PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
                Address = string.IsNullOrWhiteSpace(address) ? null : address,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            // Save user
            await _dbConnection.SaveUserAsync(newUser);
            return (true, $"User '{displayName}' has been created successfully.", newUser);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to create user: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Updates an existing user with validation.
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateUserAsync(
        User user,
        string displayName,
        string email,
        Role role,
        string? phoneNumber = null,
        string? address = null,
        string? newPassword = null)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(displayName))
            return (false, "Please enter a full name.");

        if (string.IsNullOrWhiteSpace(email))
            return (false, "Please enter an email address.");

        try
        {
            // Check if email changed and if new email already exists
            if (user.Email != email)
            {
                var existingUser = await _dbConnection.GetUserByEmailAsync(email);
                if (existingUser != null)
                    return (false, "A user with this email already exists.");
            }

            // Update user properties
            user.Email = email;
            user.DisplayName = displayName;
            user.Role = role;
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber;
            user.Address = string.IsNullOrWhiteSpace(address) ? null : address;

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword.Length < 6)
                    return (false, "Password must be at least 6 characters long.");

                var (hash, salt) = _passwordHasher.HashPassword(newPassword);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
            }

            // Save user
            await _dbConnection.SaveUserAsync(user);
            return (true, $"User '{displayName}' has been updated successfully.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to update user: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a user from the database.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteUserAsync(User user)
    {
        try
        {
            // For now, we'll mark as inactive instead of deleting
            user.IsActive = false;
            await _dbConnection.SaveUserAsync(user);
            return (true, $"User '{user.DisplayName}' has been deactivated.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete user: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _dbConnection.GetUserByEmailAsync(email);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve user by email: {email}", ex);
        }
    }
}
