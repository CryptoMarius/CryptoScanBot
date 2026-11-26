using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Funding on an open perpetual position. At every settlement the exchange moves
/// quantity x mark price x rate between the longs and the shorts: with a positive rate the longs pay
/// and the shorts receive, with a negative rate the other way round.
/// <para>
/// The amount is kept on the position (<see cref="CryptoPosition.Funding"/>), counts in its profit
/// and moves the paper quote balance. It does not move the break-even price, so the take profit and
/// the stop stay where the fills put them.
/// </para>
/// </summary>
public static class FundingTools
{
    /// <summary>
    /// The amount one settlement means for the position, in quote: received positive, paid negative.
    /// Zero for a position without an open quantity. The mark price of the settlement is used, and the
    /// last price of the symbol when the exchange did not send one.
    /// </summary>
    public static decimal Amount(CryptoPosition position, CryptoFundingRate rate)
    {
        if (position.Quantity <= 0)
            return 0m;

        decimal price = rate.MarkPrice > 0 ? rate.MarkPrice : position.Symbol.LastPrice ?? 0m;
        decimal notional = position.Quantity * price;
        decimal paidByLong = notional * rate.Rate;
        return position.Side == CryptoTradeSide.Long ? -paidByLong : paidByLong;
    }


    /// <summary>
    /// Book one settlement on an open position: the position's funding and profit, the paper balance,
    /// the database and a log line. Returns the amount booked (zero when the position has nothing open).
    /// The caller makes sure nothing else changes the position at the same moment.
    /// </summary>
    public static decimal Book(CryptoPosition position, CryptoFundingRate rate)
    {
        if (position.Status != CryptoPositionStatus.Trading)
            return 0m;

        decimal amount = Amount(position, rate);
        if (amount == 0m)
            return 0m;

        position.Funding += amount;
        TradeTools.CalculateProfitAndBreakEvenPrice(position);
        PaperAssets.BookFunding(position.Exchange, position.Symbol, amount, $"position={position.Id}");
        GlobalData.ThreadSaveObjects?.AddToQueue(position);

        string quote = position.Symbol.Quote;
        ScannerLog.Logger.Trace($"{position.Symbol.Name} position={position.Id} {position.Side} funding {rate.FundingTime:yyyy-MM-dd HH:mm} "
            + $"rate={rate.Rate.ToString0()} mark={rate.MarkPrice.ToString0()} quantity={position.Quantity.ToString0()} "
            + $"amount={amount:N4} {quote} total={position.Funding:N4} {quote}");
        return amount;
    }
}
