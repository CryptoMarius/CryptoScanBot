using Tmds.DBus;

namespace CryptoScanner.Core.Services.Linux;

public struct Secret
{
    public ObjectPath Session { get; set; }
    public byte[] Content { get; set; }
    public string ContentType { get; set; }
}
