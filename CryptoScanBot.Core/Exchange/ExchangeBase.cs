using System.Text;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.Exchange;

public abstract class ExchangeBase
{
    public static ExchangeOptions ExchangeOptions { get; } = new(); // made public for ExchangeTest project
    public abstract void ExchangeDefaults();
    public abstract Task GetSymbolsAsync();
    public abstract Task GetCandlesForSymbolAsync(CryptoSymbol symbol, long fetchEndUnix);
    public abstract Task GetCandlesForAllSymbolsAsync();

    /// <summary>
    /// return the thechnical format of the symbol on the exchange name 
    /// </summary>
    public virtual string ExchangeSymbolName(CryptoSymbol symbol)
    {
        return symbol.Name;
    }


    public abstract Task GetAssetsAsync(CryptoAccount tradeAccount);
    public abstract Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position);
    public abstract Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position);

    public abstract Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step);
    public abstract Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false);


    public static void Dump(CryptoPosition position, bool success, TradeParams tradeParams, string extraText)
    {
        StringBuilder builder = new();
        if (!success)
            builder.Append("ERROR ");
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

            if (!success)
                builder.Append($" {tradeParams.ResponseStatusCode}");
            if (!success)
                builder.Append($" {tradeParams.Error}");

            if (tradeParams.DebugJson != null)
            {
                builder.AppendLine();
                builder.AppendLine($" json={tradeParams.DebugJson}");
            }
        }

        string text = builder.ToString();

        GlobalData.AddTextToLogTab(text);
        GlobalData.AddTextToTelegram(text, position);
    }

}
