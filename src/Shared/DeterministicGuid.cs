using System.Security.Cryptography;
using System.Text;

namespace Shared;

public static class DeterministicGuid
{
    public static Guid Create(params object[] data)
    {
        var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
        var hashBytes = MD5.HashData(inputBytes);

        return new Guid(hashBytes);
    }
}