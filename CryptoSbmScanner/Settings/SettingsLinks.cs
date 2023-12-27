using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Settings;

public class CryptoExternalUrl
{
    // Alleen HyperTrader gebruikt een execute link
    public CryptoExternalUrlType Execute { get; set; } = CryptoExternalUrlType.External;
    public string Url { get; set; }
    public string Telegram { get; set; }
}

public class CryptoExternalUrls
{
    public CryptoExternalUrl Altrady { get; set; }
    public CryptoExternalUrl HyperTrader { get; set; }
    public CryptoExternalUrl TradingView { get; set; }
    public CryptoExternalUrl ExchangeUrl { get; set; }
}

public class CryptoExternalUrlList : SortedList<string, CryptoExternalUrls>
{

    public void InitializeUrls()
    {
        Add("Binance",
            new()
            {
                Altrady = new()
                {
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/BINA_{quote}_{base}:{interval}",
                },
                HyperTrader = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "hypertrader://binance/{base}-{quote}/{interval}",
                    Telegram = "http://www.ccscanner.nl/hypertrader/?e=binance&a={base}&b={quote}&i={interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=BINANCE:{base}{quote}&interval={interval}"
                },
                ExchangeUrl = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.binance.com/en/trade/{base}_{quote}?_from=markets&type=spot",
                }
            }
        );

        Add("Bybit Futures",
            new()
            {
                Altrady = new()
                {
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/BYBIF_{quote}_{base}:{interval}",
                },
                HyperTrader = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "hypertrader://bybit/{base}-{quote}/{interval}",
                    Telegram = "http://www.ccscanner.nl/hypertrader/?e=bybit&a={base}&b={quote}&i={interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=BYBIT:{base}{quote}.P&interval={interval}",
                },
                ExchangeUrl = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.bybit.com/trade/{quote}/{base}&interval={interval}",
                }
            }
        );

        Add("Bybit Spot",
            new()
            {
                Altrady = new()
                {
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/BYBI_{quote}_{base}:{interval}",
                },
                HyperTrader = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "hypertrader://bybit-spot/{base}-{quote}/{interval}",
                    Telegram = "http://www.ccscanner.nl/hypertrader/?e=bybit-spot&a={base}&b={quote}&i={interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=BYBIT:{base}{quote}&interval={interval}",
                },
                ExchangeUrl = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.bybit.com/trade/spot/{quote}/{base}&interval={interval}",
                }
            }
        );

        Add("Kucoin",
            new()
            {
                Altrady = new()
                {
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/KUCN_{quote}_{base}:{interval}",
                },
                HyperTrader = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "hypertrader://kucoin/{base}-{quote}/{interval}",
                    Telegram = "http://www.ccscanner.nl/hypertrader/?e=kucoin&a={base}&b={quote}&i={interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=KUCOIN:{base}{quote}&interval={interval}",
                },
                ExchangeUrl = new()
                {
                    // Geen idee
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.kucoin.com/trade/{quote}/{base}&interval={interval}",
                }
            }
        );
    }

    public string GetTradingAppName(CryptoTradingApp tradingApp, string exchangeName)
    {
        string text = tradingApp switch
        {
            CryptoTradingApp.Altrady => "Altrady",
            CryptoTradingApp.Hypertrader => "Hypertrader",
            CryptoTradingApp.TradingView => "TradingView",
            CryptoTradingApp.ExchangeUrl => exchangeName,
            _ => "",
        };
        return text;
    }

    //altrady://market/BINA_ETH_LOKA:2
    //http://www.ccscanner.nl/hypertrader/?e=binance&a=lto&b=usdt&i=60
    ///hypertrader://binance/BETA-BTC/5
    ///https://app.altrady.com/d/BINA_BTC_BETA:1
    ///https://app.altrady.com/d/BINA_BTC_USDT:2
    ///https://app.muunship.com/chart/BN-BETABTC?l=5&resolution=1
    ///https://www.tradingview.com/chart/?symbol=BINANCE:BETABTC&interval=1

    public (string Url, CryptoExternalUrlType Execute) GetExternalRef(CryptoTradingApp externalApp, bool telegram, CryptoSymbol symbol, CryptoInterval interval)
    {
        Model.CryptoExchange exchange = symbol.Exchange;
        if (GlobalData.Settings.General.ActivateExchange > 0)
        {
            if (!GlobalData.ExchangeListId.TryGetValue(GlobalData.Settings.General.ActivateExchange, out exchange))
                return ("", CryptoExternalUrlType.Internal);
        }

        GlobalData.LoadLinkSettings();
        if (GlobalData.ExternalUrls.TryGetValue(exchange.Name, out CryptoExternalUrls externalUrls))
        {

            CryptoExternalUrl externalUrl = externalApp switch
            {
                CryptoTradingApp.Altrady => externalUrls.Altrady,
                CryptoTradingApp.Hypertrader => externalUrls.HyperTrader,
                CryptoTradingApp.TradingView => externalUrls.TradingView,
                CryptoTradingApp.ExchangeUrl => externalUrls.ExchangeUrl,
                _ => null
            };

            if (externalUrl == null)
                return ("", CryptoExternalUrlType.Internal);


            string urlTemplate = externalUrl.Url;
            if (telegram && externalUrl.Telegram != null && externalUrl.Telegram != "")
                urlTemplate = externalUrl.Telegram;
            
            urlTemplate = urlTemplate.Replace("{name}", symbol.Name);
            urlTemplate = urlTemplate.Replace("{base}", symbol.Base);
            urlTemplate = urlTemplate.Replace("{quote}", symbol.Quote);
            
            string intervalCode = ((int)(interval.Duration / 60)).ToString();
            urlTemplate = urlTemplate.Replace("{interval}", intervalCode);
            return (urlTemplate, externalUrl.Execute);
        }

        return ("", CryptoExternalUrlType.Internal);
    }

}
