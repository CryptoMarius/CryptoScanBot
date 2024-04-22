using System.Runtime.Serialization;

namespace CryptoScanBot.Core.Intern;

public class ExchangeException : SystemException, ISerializable
{

    public ExchangeException(string message) : base(message)
    {

    }
}
