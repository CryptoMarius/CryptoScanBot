using System.Text;
using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange;

public abstract class ExchangeBase
{
    public ExchangeBase()
    {
    }

    public abstract void ExchangeDefaults();
    public abstract Task FetchSymbolsAsync();
    public abstract Task FetchCandlesAsync();


#if TRADEBOT
    public abstract Task FetchAssetsAsync(CryptoTradeAccount tradeAccount);
    public abstract Task FetchTradesForSymbolAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol);
    public abstract Task FetchTradesForOrderAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, string orderId);
    public abstract Task<(bool succes, TradeParams tradeParams)> Cancel(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoPositionStep step);

    public abstract Task<(bool result, TradeParams tradeParams)> PlaceOrder(CryptoDatabase database, 
        CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTradeSide tradeSide, DateTime currentDate,
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
            builder.Append($" {tradeParams.OrderSide}");
            builder.Append($" {tradeParams.OrderType}");
            builder.Append($" order #{tradeParams.OrderId}");
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
