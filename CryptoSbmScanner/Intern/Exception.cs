using System.Runtime.Serialization;

namespace CryptoSbmScanner.Intern;

public class ExchangeException : SystemException, ISerializable
{

    public ExchangeException(string message) : base(message)
    {

    }
}
