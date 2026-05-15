using System.Security.Cryptography;
using System.Text;

namespace JoyReactor.Accordion.Logic.Extensions;

public static class SHA256Extensions
{
    public static string ToSHA256HexString(this string data)
    {
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = SHA256.HashData(dataBytes);
        var hash = Convert.ToHexString(hashBytes);

        return hash;
    }
}