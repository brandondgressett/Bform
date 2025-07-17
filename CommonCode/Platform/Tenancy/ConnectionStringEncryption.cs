using System.Security.Cryptography;
using System.Text;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Provides encryption and decryption utilities for connection strings stored locally.
/// This adds an additional layer of security when connection strings are cached or stored temporarily.
/// </summary>
public static class ConnectionStringEncryption
{
    /// <summary>
    /// Encrypts a connection string using AES encryption.
    /// </summary>
    /// <param name="plainText">The connection string to encrypt</param>
    /// <param name="key">The encryption key (base64 encoded)</param>
    /// <returns>Base64 encoded encrypted string</returns>
    public static string Encrypt(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText));
        
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var keyBytes = Convert.FromBase64String(key);
        
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        // Combine IV and cipher text
        var resultBytes = new byte[aes.IV.Length + cipherBytes.Length];
        Array.Copy(aes.IV, 0, resultBytes, 0, aes.IV.Length);
        Array.Copy(cipherBytes, 0, resultBytes, aes.IV.Length, cipherBytes.Length);
        
        return Convert.ToBase64String(resultBytes);
    }

    /// <summary>
    /// Decrypts a connection string that was encrypted using the Encrypt method.
    /// </summary>
    /// <param name="cipherText">The encrypted connection string (base64 encoded)</param>
    /// <param name="key">The encryption key (base64 encoded)</param>
    /// <returns>The decrypted connection string</returns>
    public static string Decrypt(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentNullException(nameof(cipherText));
        
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var keyBytes = Convert.FromBase64String(key);
        var cipherBytes = Convert.FromBase64String(cipherText);
        
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        
        // Extract IV from the beginning of the cipher text
        var iv = new byte[aes.IV.Length];
        Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
        aes.IV = iv;
        
        // Extract the actual cipher text
        var actualCipherBytes = new byte[cipherBytes.Length - iv.Length];
        Array.Copy(cipherBytes, iv.Length, actualCipherBytes, 0, actualCipherBytes.Length);
        
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(actualCipherBytes, 0, actualCipherBytes.Length);
        
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Generates a new encryption key for use with the Encrypt/Decrypt methods.
    /// </summary>
    /// <returns>A base64 encoded 256-bit key</returns>
    public static string GenerateKey()
    {
        var keyBytes = new byte[32]; // 256 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    /// <summary>
    /// Validates if a string is encrypted by attempting to decrypt it.
    /// </summary>
    /// <param name="possibleCipherText">The string to check</param>
    /// <param name="key">The encryption key</param>
    /// <returns>True if the string appears to be encrypted, false otherwise</returns>
    public static bool IsEncrypted(string possibleCipherText, string key)
    {
        if (string.IsNullOrEmpty(possibleCipherText) || string.IsNullOrEmpty(key))
            return false;

        try
        {
            // Try to decrypt - if it succeeds, it was encrypted
            Decrypt(possibleCipherText, key);
            return true;
        }
        catch
        {
            // If decryption fails, it wasn't encrypted (or wrong key)
            return false;
        }
    }

    /// <summary>
    /// Securely compares two connection strings by comparing their hashes.
    /// Useful for checking if a connection string has changed without exposing the actual values.
    /// </summary>
    /// <param name="connectionString1">First connection string</param>
    /// <param name="connectionString2">Second connection string</param>
    /// <returns>True if the connection strings are identical</returns>
    public static bool SecureCompare(string connectionString1, string connectionString2)
    {
        if (connectionString1 == null && connectionString2 == null)
            return true;
        
        if (connectionString1 == null || connectionString2 == null)
            return false;

        using var sha256 = SHA256.Create();
        var hash1 = sha256.ComputeHash(Encoding.UTF8.GetBytes(connectionString1));
        var hash2 = sha256.ComputeHash(Encoding.UTF8.GetBytes(connectionString2));
        
        return hash1.SequenceEqual(hash2);
    }

    /// <summary>
    /// Creates a hash of a connection string for secure storage or comparison.
    /// </summary>
    /// <param name="connectionString">The connection string to hash</param>
    /// <returns>Base64 encoded SHA256 hash</returns>
    public static string Hash(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(connectionString));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Masks sensitive parts of a connection string for logging purposes.
    /// </summary>
    /// <param name="connectionString">The connection string to mask</param>
    /// <returns>A masked version safe for logging</returns>
    public static string MaskForLogging(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        // Common patterns to mask in connection strings
        var patterns = new[]
        {
            (@"password\s*=\s*[^;]+", "password=***"),
            (@"pwd\s*=\s*[^;]+", "pwd=***"),
            (@"accountkey\s*=\s*[^;]+", "accountkey=***"),
            (@"accesskey\s*=\s*[^;]+", "accesskey=***"),
            (@"sharedaccesskey\s*=\s*[^;]+", "sharedaccesskey=***"),
            (@"token\s*=\s*[^;]+", "token=***"),
            (@"secret\s*=\s*[^;]+", "secret=***")
        };

        var masked = connectionString;
        foreach (var (pattern, replacement) in patterns)
        {
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked, 
                pattern, 
                replacement, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return masked;
    }
}

/// <summary>
/// Extension methods for working with encrypted connection strings.
/// </summary>
public static class ConnectionStringEncryptionExtensions
{
    /// <summary>
    /// Encrypts the connection string in a TenantConnection object.
    /// </summary>
    public static void EncryptConnectionString(this TenantConnection connection, string key, string? plainConnectionString)
    {
        if (!string.IsNullOrEmpty(plainConnectionString))
        {
            connection.EncryptedConnectionString = ConnectionStringEncryption.Encrypt(plainConnectionString, key);
        }
    }

    /// <summary>
    /// Decrypts the connection string from a TenantConnection object.
    /// </summary>
    public static string? DecryptConnectionString(this TenantConnection connection, string key)
    {
        if (string.IsNullOrEmpty(connection.EncryptedConnectionString))
            return null;

        return ConnectionStringEncryption.Decrypt(connection.EncryptedConnectionString, key);
    }

    /// <summary>
    /// Checks if the connection string in a TenantConnection is encrypted.
    /// </summary>
    public static bool IsConnectionStringEncrypted(this TenantConnection connection, string key)
    {
        return ConnectionStringEncryption.IsEncrypted(connection.EncryptedConnectionString, key);
    }
}