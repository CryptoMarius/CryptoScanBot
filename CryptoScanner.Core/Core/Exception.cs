using System.Runtime.Serialization;

namespace CryptoScanner.Core.Core;

public class ExchangeException(string message) : SystemException(message), ISerializable
{
}
