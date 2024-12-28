using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

public class CryptoWhatever
{
    public required virtual CryptoSymbol Symbol { get; set; }
    public required virtual CryptoInterval Interval { get; set; }
}
