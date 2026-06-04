using System.Security.Cryptography;
using System.Text;

namespace PixPro.Services.Notifications.API.Helpers;

public static class UserIdHelper
{
    public static string DeriveUserId(string sub)
    {
        if (Guid.TryParse(sub, out var guid))
            return guid.ToString();

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sub));
        var guidBytes = hash[..16];
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes).ToString();
    }
}
