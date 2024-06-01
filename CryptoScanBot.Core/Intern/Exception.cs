using System.Runtime.Serialization;

namespace CryptoScanBot.Core.Intern;

public class ExchangeException(string message) : SystemException(message), ISerializable
{
}
