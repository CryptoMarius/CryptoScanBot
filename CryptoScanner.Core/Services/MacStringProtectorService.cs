using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace CryptoScanner.Core.Services;

[SupportedOSPlatform("macos")]
public class MacStringProtectorService : IStringProtectorService
{
    private const string ServiceName = "CryptoScanner";
    private const string AccountName = "JsonEncryptionKey";

    public string Protect(string plaintext)
    {
        var key = GetOrCreateKey();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);

        using var encryptor = aes.CreateEncryptor();
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Layout: [IV][Ciphertext]
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Unprotect(string ciphertext)
    {
        var key = GetOrCreateKey();
        var full = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[full.Length - iv.Length];

        Buffer.BlockCopy(full, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(full, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private byte[] GetOrCreateKey()
    {
        if (TryReadKey(out var key))
            return key;

        key = RandomNumberGenerator.GetBytes(32); // 256‑bit
        SaveKey(key);
        return key;
    }

    private bool TryReadKey(out byte[] key)
    {
        key = Array.Empty<byte>();

        var status = SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)ServiceName.Length, ServiceName,
            (uint)AccountName.Length, AccountName,
            out uint passwordLength,
            out IntPtr passwordData,
            out IntPtr itemRef);

        if (status != 0)
            return false;

        try
        {
            key = new byte[passwordLength];
            Marshal.Copy(passwordData, key, 0, (int)passwordLength);
            return true;
        }
        finally
        {
            SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            if (itemRef != IntPtr.Zero)
                CFRelease(itemRef);
        }
    }

    private void SaveKey(byte[] key)
    {
        var status = SecKeychainAddGenericPassword(
            IntPtr.Zero,
            (uint)ServiceName.Length, ServiceName,
            (uint)AccountName.Length, AccountName,
            (uint)key.Length, key,
            out IntPtr itemRef);

        if (itemRef != IntPtr.Zero)
            CFRelease(itemRef);

        if (status != 0)
            throw new InvalidOperationException($"Failed to add key to Keychain, status: {status}");
    }

    // P/Invoke Security.framework

    [DllImport("/System/Library/Frameworks/Security.framework/Security")]
    private static extern int SecKeychainAddGenericPassword(
        IntPtr keychain,
        uint serviceNameLength, string serviceName,
        uint accountNameLength, string accountName,
        uint passwordLength, byte[] passwordData,
        out IntPtr itemRef);

    [DllImport("/System/Library/Frameworks/Security.framework/Security")]
    private static extern int SecKeychainFindGenericPassword(
        IntPtr keychain,
        uint serviceNameLength, string serviceName,
        uint accountNameLength, string accountName,
        out uint passwordLength,
        out IntPtr passwordData,
        out IntPtr itemRef);

    [DllImport("/System/Library/Frameworks/Security.framework/Security")]
    private static extern int SecKeychainItemFreeContent(
        IntPtr attrList,
        IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);
}
