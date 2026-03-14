using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace CryptoScanner.Core.Services;

[SupportedOSPlatform("windows")]
public class WindowsStringProtectorService : IStringProtectorService
{
    public string Protect(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);

        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);

        return Convert.ToBase64String(encrypted);
    }

    public string Unprotect(string ciphertext)
    {
        var encryptedBytes = Convert.FromBase64String(ciphertext);

        var decrypted = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);

        return Encoding.UTF8.GetString(decrypted);
    }
}
