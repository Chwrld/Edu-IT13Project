#!/usr/bin/env dotnet-script
// Script to generate PBKDF2-SHA256 password hashes for test users
// Usage: dotnet script generate_test_hashes.csx

using System.Security.Cryptography;
using System.Text;

const int SaltSize = 16;
const int HashSize = 32;
const int Iterations = 10000;

(string Hash, string Salt) HashPassword(string password)
{
    using var rng = RandomNumberGenerator.Create();
    var saltBytes = new byte[SaltSize];
    rng.GetBytes(saltBytes);

    using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
    var hashBytes = pbkdf2.GetBytes(HashSize);

    var hash = Convert.ToBase64String(hashBytes);
    var salt = Convert.ToBase64String(saltBytes);

    return (hash, salt);
}

// Generate hashes for test users
var passwords = new Dictionary<string, string>
{
    { "student@university.edu", "student@123" },
    { "teacher@university.edu", "teacher@123" },
    { "admin@university.edu", "admin@123" }
};

Console.WriteLine("Generated Password Hashes for Test Users:");
Console.WriteLine("==========================================\n");

foreach (var (email, password) in passwords)
{
    var (hash, salt) = HashPassword(password);
    Console.WriteLine($"Email: {email}");
    Console.WriteLine($"Password: {password}");
    Console.WriteLine($"Hash: {hash}");
    Console.WriteLine($"Salt: {salt}");
    Console.WriteLine($"SQL: (NEWID(), '{email}', '{hash}', '{salt}', ...");
    Console.WriteLine();
}
