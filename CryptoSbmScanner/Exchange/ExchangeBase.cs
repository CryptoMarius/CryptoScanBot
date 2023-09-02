using System.Text;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

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
    public abstract Task FetchTradesAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol);


    private static string DumpOrder(CryptoSymbol symbol, TradeParams tradeParams, string extraText)
    {
        StringBuilder builder = new();
        builder.Append(symbol.Name);
        builder.Append($" {tradeParams.OrderSide}");
        if (extraText != "")
            builder.Append($" {extraText}");
        //builder.Append($"{symbol.Name} Position {tradeParams.OrderSide}");
        builder.Append($" order #{tradeParams.OrderId}");
        builder.Append($" price={tradeParams.Price.ToString0()}");
        if (tradeParams.StopPrice.HasValue)
            builder.Append($" stop={tradeParams.StopPrice?.ToString0()}");
        builder.Append($" quantity={tradeParams.Quantity.ToString0()}");
        _ = builder.Append($" value={tradeParams.QuoteQuantity.ToString(symbol.QuoteData.DisplayFormat)}");

        return builder.ToString();
    }

    private static string DumpError(CryptoSymbol symbol, TradeParams tradeParams, string extraText)
    {
        StringBuilder builder = new();
        builder.Append("ERROR ");
        builder.Append(DumpOrder(symbol, tradeParams, extraText));
        builder.Append($" {tradeParams.ResponseStatusCode}");
        builder.Append($" {tradeParams.Error}");

        return builder.ToString();
    }

    public static void Dump(CryptoSymbol symbol, bool actionOkay, TradeParams tradeParams, string extraText)
    {
        string text;

        if (actionOkay)
            text = DumpOrder(symbol, tradeParams, extraText);
        else
            text = DumpError(symbol, tradeParams, extraText);

        GlobalData.AddTextToLogTab(text);
        GlobalData.AddTextToTelegram(text);
    }

#endif

}
