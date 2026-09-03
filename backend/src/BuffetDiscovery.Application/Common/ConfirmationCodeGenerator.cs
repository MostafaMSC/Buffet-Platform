using System.Security.Cryptography;

namespace BuffetDiscovery.Application.Common;

/// Short, human-shareable codes for the customer's booking "badge" — no ambiguous
/// characters (0/O, 1/I) since these get read aloud at a restaurant door.
public static class ConfirmationCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate(int length = 7)
    {
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return new string(chars);
    }
}
