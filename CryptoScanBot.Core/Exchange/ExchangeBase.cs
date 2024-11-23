using System.Text;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.Exchange;

// todo: Introduce units for the interfaces

public interface IOrder
{
    public Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position);
}

public interface ITrade
{
    public Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position);
}

public interface IAsset
{
    public Task GetAssetsAsync(CryptoAccount tradeAccount);
}

public interface ISymbol
{
    public Task GetSymbolsAsync();
}

public interface ICandle
{
    public Task GetCandlesForIntervalAsync(IDisposable? clientBase, CryptoSymbol symbol, CryptoInterval interval, long fetchEndUnix);
    public Task GetCandlesForAllIntervalsAsync(CryptoSymbol symbol, long fetchEndUnix);
    public Task GetCandlesAsync();
}

public interface IApi
{
    public IAsset Asset { get; set; }
    public ICandle Candle { get; set; }
    public ISymbol Symbol { get; set; }
    public IOrder Order { get; set; }
    public ITrade Trade { get; set; }
    //Rates?
}


// Name: ExchangeBroker?
public abstract class ExchangeBase
{
    public required IAsset Asset { get; set; }
    public required ICandle Candle { get; set; }
    public required ISymbol Symbol { get; set; }
    public required IOrder Order { get; set; }
    public required ITrade Trade { get; set; }

    public abstract IDisposable GetClient();
    public abstract void ExchangeDefaults();

    public static Ticker? PriceTicker { get; set; }
    public static Ticker? KLineTicker { get; set; }
    public static Ticker? UserTicker { get; set; }

    public static ExchangeOptions ExchangeOptions { get; } = new(); // made public for ExchangeTest project
    public static CancellationTokenSource CancellationTokenSource { get; set; } = new();
    public static CancellationToken CancellationToken { get; set; } = CancellationTokenSource.Token;

    /// <summary>
    /// return the thechnical format of the symbol on the exchange name 
    /// </summary>
    public virtual string ExchangeSymbolName(CryptoSymbol symbol)
    {
        return symbol.Name;
    }

    public abstract Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step);
    public abstract Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false);

    public static void Dump(CryptoPosition position, bool success, TradeParams? tradeParams, string extraText)
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
            _ = builder.Append($" value={tradeParams.QuoteQuantity.ToString(position.Symbol.QuoteData!.DisplayFormat)}");

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
