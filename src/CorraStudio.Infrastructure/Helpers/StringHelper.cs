using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CorraStudio.Infrastructure.Helpers;

public static class StringHelper
{
    public static string GenerateRandomCode(int length = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static string GenerateSecureToken(int length = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..length];
    }

    public static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = md5.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Truncate(string? input, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        
        if (input.Length <= maxLength)
            return input;
        
        return input[..maxLength] + suffix;
    }

    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;
        
        var regex = new Regex(@"^(\+62|62|0)8[1-9][0-9]{6,10}$");
        return regex.IsMatch(phoneNumber);
    }

    public static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        
        var slug = text.ToLowerInvariant();
        slug = slug.Replace(" ", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        
        return slug;
    }
}
