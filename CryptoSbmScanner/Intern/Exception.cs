using System;
using System.Runtime.Serialization;

namespace CryptoSbmScanner
{
    public class ExchangeException : SystemException, ISerializable
    {

        public ExchangeException(string message) : base(message)
        {

        }
    }

}
