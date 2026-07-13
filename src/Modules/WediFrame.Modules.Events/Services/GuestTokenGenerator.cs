using System.Buffers.Text;
using System.Security.Cryptography;

namespace WediFrame.Modules.Events.Services;

/// <summary>
/// Generates long, unguessable, URL-safe guest access tokens.
/// 32 random bytes -> Base64Url = 43 chars, ~256 bits of entropy.
/// Never sequential, never derived from event data.
/// </summary>
public static class GuestTokenGenerator
{
    public static string NewToken()
        => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
}
