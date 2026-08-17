using System.Security.Cryptography;
using System.Text;

namespace TaskManagement.Application.Common.Security;

public static class RefreshTokenHasher
{
    public static string Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public static string Hash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }
}
