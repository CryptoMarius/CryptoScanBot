namespace CryptoScanner.Core.Model;

/// <summary>
/// One funding settlement of a perpetual: the moment the exchange settled it, the rate and the mark
/// price it was settled on. A positive rate means the longs pay the shorts, a negative rate the other
/// way round. The amount of one position is quantity x mark price x rate (see
/// <see cref="Trader.FundingTools"/>).
/// </summary>
public readonly record struct CryptoFundingRate(DateTime FundingTime, decimal Rate, decimal MarkPrice);
