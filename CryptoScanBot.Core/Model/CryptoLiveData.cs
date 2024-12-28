using CryptoScanBot.Core.Signal;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

public class CryptoLiveData
{
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoInterval Interval { get; set; }
}
