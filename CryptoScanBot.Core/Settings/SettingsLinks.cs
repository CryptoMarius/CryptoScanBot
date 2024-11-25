using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Settings;

public class CryptoExternalUrl
{
    // Alleen HyperTrader gebruikt een execute link
    public CryptoExternalUrlType Execute { get; set; } = CryptoExternalUrlType.External;
    public string? Url { get; set; }
    public string? Telegram { get; set; }
}

public class CryptoExternalUrlAltrady : CryptoExternalUrl
{
    public string? Code { get; set; }
}

public class CryptoExternalUrls
{
    public CryptoExternalUrlAltrady? Altrady { get; set; }
    public CryptoExternalUrl? HyperTrader { get; set; }
    public CryptoExternalUrl? TradingView { get; set; }
    public CryptoExternalUrl? ExchangeUrl { get; set; }
}

public class CryptoExternalUrlList : SortedList<string, CryptoExternalUrls>
{

    /// <summary>
    /// Defaults for the url's
    /// </summary>
    public void InitializeUrls()
    {
        Remove("Binance");
        this.TryAdd("Binance Futures",
            new()
            {
                Altrady = new() // werkt niet
                {
                    Code = "BIFU",
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/BIFU_{QUOTE}_{BASE}:{interval}",
                },
                HyperTrader = new() // werkt niet
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "hypertrader://binance/{BASE}-{QUOTE}/{interval}",
                    Telegram = "http://www.ccscanner.nl/hypertrader/?e=binance&a={BASE}&b={QUOTE}&i={interval}",
                },
                TradingView = new() // werkt
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=BINANCE:{BASE}{QUOTE}.P&interval={interval}"
                },
                ExchangeUrl = new() // werkt niet
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.binance.com/en/trade/{BASE}_{QUOTE}?_from=markets&type=futures",
                }
            }
        );

        this.TryAdd("Binance Spot",
            new()
            {
                Altrady = new()
                {
                    Code = "BINA",
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/BINA_{QUOTE}_{BASE}:{interval}",
                },
                HyperTrader = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "hypertrader://binance/{BASE}-{QUOTE}/{interval}",
                    Telegram = "http://www.ccscanner.nl/hypertrader/?e=binance&a={BASE}&b={QUOTE}&i={interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=BINANCE:{BASE}{QUOTE}&interval={interval}"
                },
                ExchangeUrl = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.binance.com/en/trade/{BASE}_{QUOTE}?_from=markets&type=spot",
                }
            }
        );


        Remove("Bybit");
        this.TryAdd("Bybit Futures",
            new()
            {
                Altrady = new()
                {
                    Code = "BYBIF",
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/BYBIF_{QUOTE}_{BASE}:{interval}",
                },
                HyperTrader = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "hypertrader://bybit/{BASE}-{QUOTE}/{interval}",
                    Telegram = "http://www.ccscanner.nl/hypertrader/?e=bybit&a={BASE}&b={QUOTE}&i={interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=BYBIT:{BASE}{QUOTE}.P&interval={interval}",
                },
                ExchangeUrl = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.bybit.com/trade/{quote}/{BASE}{QUOTE}",
                }
            }
        );

        this.TryAdd("Bybit Spot",
            new()
            {
                Altrady = new()
                {
                    Code = "BYBI",
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/BYBI_{QUOTE}_{BASE}:{interval}",
                },
                HyperTrader = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "hypertrader://bybit-spot/{BASE}-{QUOTE}/{interval}",
                    Telegram = "http://www.ccscanner.nl/hypertrader/?e=bybit-spot&a={BASE}&b={QUOTE}&i={interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=BYBIT:{BASE}{QUOTE}&interval={interval}",
                },
                ExchangeUrl = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.bybit.com/trade/spot/{BASE}/{QUOTE}",
                }
            }
        );

        Remove("Kucoin");
        this.TryAdd("Kucoin Futures",
            new()
            {
                Altrady = new()
                {
                    Code = "KUCNF",
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/KUCNF_{QUOTE}_{BASE}:{interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=KUCOIN:{BASE}{QUOTE}.P&interval={interval}",
                },
            }
        );
        this.TryAdd("Kucoin Spot",
            new()
            {
                Altrady = new()
                {
                    Code = "KUCN",
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/KUCN_{QUOTE}_{BASE}:{interval}",
                },
                HyperTrader = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "hypertrader://kucoin/{BASE}-{QUOTE}/{interval}",
                    Telegram = "http://www.ccscanner.nl/hypertrader/?e=kucoin&a={BASE}&b={QUOTE}&i={interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=KUCOIN:{BASE}{QUOTE}&interval={interval}",
                },
                ExchangeUrl = new()
                {
                    // Geen idee
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.kucoin.com/trade/{QUOTE}/{BASE}&interval={interval}",
                }
            }
        );

        Remove("Kraken");
        this.TryAdd("Kraken Spot",
            new()
            {
                Altrady = new()
                {
                    Code = "KRKN",
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/KRKN_{QUOTE}_{BASE}:{interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=KRAKEN:{BASE}{QUOTE}&interval={interval}",
                },
            }
        );


        Remove("Mexc");
        this.TryAdd("Mexc Spot",
            new()
            {
                Altrady = new()
                {
                    Code = "MEXC",
                    Execute = CryptoExternalUrlType.Internal,
                    Url = "https://app.altrady.com/d/MEXC_{QUOTE}_{BASE}:{interval}",
                },
                TradingView = new()
                {
                    Execute = CryptoExternalUrlType.External,
                    Url = "https://www.tradingview.com/chart/?symbol=MEXC:{BASE}{QUOTE}&interval={interval}",
                },
            }
        );


    }

    public static string GetTradingAppName(CryptoTradingApp tradingApp, string exchangeName)
    {
        string text = tradingApp switch
        {
            CryptoTradingApp.Altrady => $"Altrady {exchangeName}",
            CryptoTradingApp.Hypertrader => $"Hypertrader {exchangeName}",
            CryptoTradingApp.TradingView => $"TradingView {exchangeName}",
            CryptoTradingApp.ExchangeUrl => $"Exchange {exchangeName}",
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
        if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ActivateExchangeName, out Model.CryptoExchange? exchange))
            exchange = GlobalData.Settings.General.Exchange;
        return GetExternalRef(exchange!, externalApp, telegram, symbol, interval);
    }


    public bool GetExternalRef(Model.CryptoExchange exchange, out CryptoExternalUrls? externalUrls)
    {
        return TryGetValue(exchange.Name, out externalUrls);
    }

    public (string Url, CryptoExternalUrlType Execute) GetExternalRef(Model.CryptoExchange exchange, CryptoTradingApp externalApp, bool telegram, CryptoSymbol symbol, CryptoInterval interval)
    {
        if (GetExternalRef(exchange, out CryptoExternalUrls? externalUrls0))
        {
            CryptoExternalUrls externalUrls = externalUrls0!;

            CryptoExternalUrl? externalUrl = externalApp switch
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

            urlTemplate = urlTemplate.Replace("{name}", symbol.Name.ToLower());
            urlTemplate = urlTemplate.Replace("{base}", symbol.Base.ToLower());
            urlTemplate = urlTemplate.Replace("{quote}", symbol.Quote.ToLower());

            urlTemplate = urlTemplate.Replace("{NAME}", symbol.Name.ToUpper());
            urlTemplate = urlTemplate.Replace("{BASE}", symbol.Base.ToUpper());
            urlTemplate = urlTemplate.Replace("{QUOTE}", symbol.Quote.ToUpper());

            string intervalCode = ((int)(interval.Duration / 60)).ToString();
            urlTemplate = urlTemplate.Replace("{interval}", intervalCode.ToLower());
            urlTemplate = urlTemplate.Replace("{INTERVAL}", intervalCode.ToUpper());
            return (urlTemplate, externalUrl.Execute);
        }

        return ("", CryptoExternalUrlType.Internal);
    }

}
