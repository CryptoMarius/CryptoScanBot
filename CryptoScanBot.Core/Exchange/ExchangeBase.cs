using System.Text;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.Exchange;

public abstract class ExchangeBase
{
    internal static ExchangeOptions ExchangeOptions { get; } = new();
    public abstract void ExchangeDefaults();
    public abstract Task GetSymbolsAsync();
    public abstract Task GetCandlesAsync();

    /// <summary>
    /// return the thechnical format of the symbol on the exchange name 
    /// </summary>
    public virtual string ExchangeSymbolName(CryptoSymbol symbol)
    {
        return symbol.Name;
    }


#if TRADEBOT
    public abstract Task GetAssetsAsync(CryptoTradeAccount tradeAccount);
    public abstract Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position);
    public abstract Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position);

    public abstract Task<(bool succes, TradeParams tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step);
    public abstract Task<(bool result, TradeParams tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, CryptoTradeSide tradeSide, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit);

    private static string DumpOrder(CryptoPosition position, TradeParams tradeParams, string extraText)
    {
        StringBuilder builder = new();
        builder.Append(position.Symbol.Name);
        if (extraText != "")
            builder.Append($" {extraText}");
        if (tradeParams != null)
        {
            builder.Append($" {tradeParams.Purpose.ToString().ToLower()}");
            builder.Append($" {tradeParams.OrderSide.ToString().ToLower()}");
            builder.Append($" {tradeParams.OrderType.ToString().ToLower()}");
            builder.Append($" order={tradeParams.OrderId}");
            builder.Append($" price={tradeParams.Price.ToString0()}");
            if (tradeParams.StopPrice.HasValue)
                builder.Append($" stop={tradeParams.StopPrice?.ToString0()}");
            builder.Append($" quantity={tradeParams.Quantity.ToString0()}");
            _ = builder.Append($" value={tradeParams.QuoteQuantity.ToString(position.Symbol.QuoteData.DisplayFormat)}");
        }

        return builder.ToString();
    }

    private static string DumpError(CryptoPosition position, TradeParams tradeParams, string extraText)
    {
        StringBuilder builder = new();
        builder.Append("ERROR ");
        builder.Append(DumpOrder(position, tradeParams, extraText));
        if (tradeParams != null)
        {
            builder.Append($" {tradeParams.ResponseStatusCode}");
            builder.Append($" {tradeParams.Error}");
        }

        return builder.ToString();
    }

    public static void Dump(CryptoPosition position, bool success, TradeParams tradeParams, string extraText)
    {
        string text;

        if (success)
            text = DumpOrder(position, tradeParams, extraText);
        else
            text = DumpError(position, tradeParams, extraText);

        GlobalData.AddTextToLogTab(text);
        GlobalData.AddTextToTelegram(text, position);
    }

#endif

}
