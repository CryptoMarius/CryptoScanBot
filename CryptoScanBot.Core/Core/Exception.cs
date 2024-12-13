using System.Runtime.Serialization;

namespace CryptoScanBot.Core.Core;

public class ExchangeException(string message) : SystemException(message), ISerializable
{
}
