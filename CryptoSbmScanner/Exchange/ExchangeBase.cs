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



    public static void DumpOrder(CryptoSymbol symbol, TradeParams tradeParams, string extraText)
    {
        string text = string.Format("{0} POSITION {1} {2} ORDER #{3} {4} PLACED price={5} stop={6} quantity={7} quotequantity={8}",
            symbol.Name, tradeParams.OrderSide,
            tradeParams.OrderType.ToString(),
            tradeParams.OrderId,
            extraText,
            tradeParams.Price.ToString0(),
            tradeParams.StopPrice?.ToString0(),
            tradeParams.Quantity.ToString0(),
            tradeParams.QuoteQuantity.ToString0());
        GlobalData.AddTextToLogTab(text);
        GlobalData.AddTextToTelegram(text);
    }

    public static void DumpError(CryptoSymbol symbol, TradeParams tradeParams, string extraText)
    {
        string text = string.Format("{0} ERROR {1} {2} order {3} {4} {5}\r\n", symbol.Name, tradeParams.OrderType, 
            tradeParams.OrderSide, tradeParams.ResponseStatusCode, tradeParams.Error, extraText);
        text += string.Format("quantity={0}\r\n", tradeParams.Quantity.ToString0());
        text += string.Format("price={0}\r\n", tradeParams.Price.ToString0());
        if (tradeParams.StopPrice.HasValue)
            text += string.Format("stop={0}\r\n", tradeParams.StopPrice?.ToString0());
        if (tradeParams.LimitPrice.HasValue)
            text += string.Format("limit={0}\r\n", tradeParams.LimitPrice?.ToString0());
        //text += string.Format("lastprice={0}\r\n", Symbol.LastPrice?.ToString0());
        //text += string.Format("trades={0}\r\n", Symbol.TradeList.Count);
        GlobalData.AddTextToLogTab(text);
        GlobalData.AddTextToTelegram(text);
    }

#endif

}
