using System.Data;
using System.Data.SqlClient;
using MauiAppIT13.Models;

namespace MauiAppIT13.Database;

/// <summary>
/// Abstract base class for database connections.
/// Implement this to support different storage backends (SQL Server, in-memory, etc.)
/// </summary>
public abstract class DbConnection
{
    public abstract Task<bool> HasAnyUsersAsync();
    public abstract Task<User?> GetUserByEmailAsync(string email);
    public abstract Task<IReadOnlyCollection<User>> GetUsersAsync();
    public abstract Task SaveUserAsync(User user);
    public virtual IDbConnection GetConnection() => throw new NotImplementedException("GetConnection not implemented for this connection type");
}

/// <summary>
/// In-memory implementation - works immediately without SQL Server.
/// </summary>
public sealed class InMemoryDbConnection : DbConnection
{
    private static readonly List<User> Users = new();

    public override async Task<bool> HasAnyUsersAsync()
    {
        return await Task.FromResult(Users.Any());
    }

    public override async Task<User?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var user = Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(user);
    }

    public override async Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        return await Task.FromResult(Users.AsReadOnly());
    }

    public override async Task SaveUserAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("User email is required", nameof(user));

        var existingUser = Users.FirstOrDefault(u => u.Id == user.Id);
        if (existingUser is not null)
        {
            Users.Remove(existingUser);
        }

        Users.Add(user);
        await Task.CompletedTask;
    }
}
