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
    public abstract Task FetchTradesAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol);



    internal void DumpOrder(CryptoSymbol symbol, TradeParams tradeParams, string extraText)
    {
        string text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} {4} PLACED price={5} stop={6} quantity={7} quotequantity={8}",
            symbol.Name, tradeParams.Side,
            tradeParams.OrderType.ToString(),
            tradeParams.OrderId,
            extraText,
            tradeParams.Price.ToString0(),
            tradeParams.StopPrice?.ToString0(),
            tradeParams.Quantity.ToString0(),
            tradeParams.QuoteQuantity.ToString0());
        GlobalData.AddTextToLogTab(text2);
        GlobalData.AddTextToTelegram(text2);
    }

    internal void DumpError(CryptoSymbol symbol, CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, string extraText,
        string responseStatusCode, string error)
    {
        string text = string.Format("{0} ERROR {1} {2} order {3} {4} {5}\r\n", symbol.Name, orderType, orderSide, responseStatusCode, error, extraText);
        text += string.Format("quantity={0}\r\n", quantity.ToString0());
        text += string.Format("price={0}\r\n", price.ToString0());
        if (stop.HasValue)
            text += string.Format("stop={0}\r\n", stop?.ToString0());
        if (limit.HasValue)
            text += string.Format("limit={0}\r\n", limit?.ToString0());
        //text += string.Format("lastprice={0}\r\n", Symbol.LastPrice?.ToString0());
        //text += string.Format("trades={0}\r\n", Symbol.TradeList.Count);
        GlobalData.AddTextToLogTab(text);
        GlobalData.AddTextToTelegram(text);
    }

#endif

}
