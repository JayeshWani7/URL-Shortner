using System.Security.Cryptography;

namespace UrlShortener.Services;

public static class ShortCodeGenerator
{
    private const string Characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Generate(int length = 6)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Characters[bytes[i] % Characters.Length];
        }

        return new string(chars);
    }
}
