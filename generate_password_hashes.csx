#!/usr/bin/env dotnet-script
// Generate PBKDF2-SHA256 password hashes for role@123 passwords

using System;
using System.Security.Cryptography;
using System.Text;
using System.Convert;

// PasswordHasher implementation (same as in the app)
public class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10000;

    public (string hash, string salt) HashPassword(string password)
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] saltBytes = new byte[SaltSize];
            rng.GetBytes(saltBytes);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hashBytes = pbkdf2.GetBytes(HashSize);
                
                string hashBase64 = Convert.ToBase64String(hashBytes);
                string saltBase64 = Convert.ToBase64String(saltBytes);
                
                return (hashBase64, saltBase64);
            }
        }
    }
}

// Generate hashes
var hasher = new PasswordHasher();

Console.WriteLine("========================================");
Console.WriteLine("PBKDF2-SHA256 Password Hashes");
Console.WriteLine("========================================\n");

var passwords = new[] 
{
    ("admin@123", "admin@university.edu"),
    ("teacher@123", "teacher@university.edu"),
    ("adviser@123", "adviser@university.edu"),
    ("student@123", "student@university.edu")
};

foreach (var (password, email) in passwords)
{
    var (hash, salt) = hasher.HashPassword(password);
    Console.WriteLine($"Email: {email}");
    Console.WriteLine($"Password: {password}");
    Console.WriteLine($"Hash: {hash}");
    Console.WriteLine($"Salt: {salt}");
    Console.WriteLine();
}

Console.WriteLine("========================================");
Console.WriteLine("Copy these values into UpdatePasswords.sql");
Console.WriteLine("========================================");
