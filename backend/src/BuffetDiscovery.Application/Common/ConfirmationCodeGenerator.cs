using System.Security.Cryptography;
using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common;

/// Short, human-shareable booking references — no ambiguous characters (0/O, 1/I) since
/// these get read aloud at a restaurant door. Prefixed by service type so staff can tell a
/// buffet booking from a set menu booking at a glance: BUF-7K2M9, SET-3QX4P.
public static class ConfirmationCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate(ServiceType serviceType, int length = 5)
    {
        var prefix = serviceType == ServiceType.SetMenu ? "SET" : "BUF";
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return $"{prefix}-{new string(chars)}";
    }
}
