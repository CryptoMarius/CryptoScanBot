using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;

public class Altrady
{
    //https://app.altrady.com/d/BINA_BTC_ANKR:2
    //hypertrader://binance/BETA-BTC/2
    //https://app.muunship.com/chart/BN-ANKRBTC?l=5&resolution=2
    //https://www.tradingview.com/chart/?symbol=BINANCE:ANKRBTC&interval=2

    // Deze lijst matched nog niet volledig (met name de hogere intervallen niet getest)
    private static readonly string[] AltradyInterval = new[]
    { "1", "2", "3", "5", "10", "15", "30", "60", "120", "240", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1" };


    static public string GetRef(CryptoSymbol symbol, CryptoInterval interval)
    {
        string href;
        if (symbol.ExchangeId == 1)
            href = string.Format("https://app.altrady.com/d/BINA_{0}_{1}:{2}", symbol.Quote,
                symbol.Base, AltradyInterval[(int)interval.IntervalPeriod]);
        else
            href = string.Format("https://app.altrady.com/d/BYBI_{0}_{1}:{2}", symbol.Quote,
            symbol.Base, AltradyInterval[(int)interval.IntervalPeriod]);
        return href;
    }
}


public class HyperTrader
{
    // Deze lijst matched nog niet volledig (met name de hogere intervallen niet getest)
    private static readonly string[] HypertraderInterval = new[]
    { "1", "2", "3", "5", "10", "15", "30", "60", "120", "240", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1" };

    static public string GetRef(CryptoSymbol symbol, CryptoInterval interval)
    {
        ///hypertrader://binance/BETA-BTC/5
        ///https://app.altrady.com/d/BINA_BTC_BETA:1
        ///https://app.muunship.com/chart/BN-BETABTC?l=5&resolution=1
        ///https://www.tradingview.com/chart/?symbol=BINANCE:BETABTC&interval=1
        //altrady://market/BINA_BUSD_LOKA:2

        //http://www.ccscanner.nl/hypertrader/?e=binance&a=lto&b=usdt&i=60
        string href;
        if (symbol.ExchangeId == 1)
            href = string.Format("hypertrader://binance/{0}-{1}/{2}", symbol.Base, symbol.Quote,
                HypertraderInterval[(int)interval.IntervalPeriod]);
        else
            href = string.Format("hypertrader://bybit/{0}-{1}/{2}", symbol.Base, symbol.Quote,
                HypertraderInterval[(int)interval.IntervalPeriod]);
        return href.ToLower();
    }
}


public class TradingView
{
    // Deze lijst matched nog niet volledig (met name de hogere intervallen niet getest)
    private static readonly string[] TradingViewInterval = new[]
    { "1", "2", "3", "5", "10", "15", "30", "60", "120", "240", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1" };

    static public string GetRef(CryptoSymbol symbol, CryptoInterval interval)
    {
        ///hypertrader://binance/BETA-BTC/5
        ///https://app.altrady.com/d/BINA_BTC_BETA:1
        ///https://app.muunship.com/chart/BN-BETABTC?l=5&resolution=1
        ///https://www.tradingview.com/chart/?symbol=BINANCE:BETABTC&interval=1
        //altrady://market/BINA_BUSD_LOKA:2

        string href;
        if (symbol.ExchangeId == 1)
            href = string.Format("https://www.tradingview.com/chart/?symbol=BINANCE:{0}&interval={1}", symbol.Name,
                TradingViewInterval[(int)interval.IntervalPeriod]);
        else
            href = string.Format("https://www.tradingview.com/chart/?symbol=BYBIT:{0}&interval={1}", symbol.Name,
                TradingViewInterval[(int)interval.IntervalPeriod]);
        return href;
    }
}
