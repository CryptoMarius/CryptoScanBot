using CryptoSbmScanner.Enums;

namespace CryptoSbmScanner.Settings;


[Serializable]
/// Controle hoe een munt zich gedraagt
public class PauseTradingRule
{
    public string Symbol { get; set; }
    public double Percentage { get; set; }
    public int Candles { get; set; }
    public CryptoIntervalPeriod Interval { get; set; }

    // de wachttijd gemeten in candles
    public int CoolDown { get; set; }
}

public class SettingsTradingBase
{
    // Is deze trade richting actief
    public bool Active { get; set; } = true;

    //***************************
    // Barometer
    // Geen nieuwe posities openen als de barometer onder een van deze getallen staat
    public decimal Barometer15mBotMinimal { get; set; }
    public decimal Barometer30mBotMinimal { get; set; }
    public decimal Barometer01hBotMinimal { get; set; }
    public decimal Barometer04hBotMinimal { get; set; }
    public decimal Barometer24hBotMinimal { get; set; }
}

public class SettingsTradingLong : SettingsTradingBase
{
    public SettingsTradingLong()
    {
        Barometer15mBotMinimal = -0.5m;
        Barometer30mBotMinimal = -0.5m;
        Barometer01hBotMinimal = 0.3m;
        Barometer04hBotMinimal = -1.0m;
        Barometer24hBotMinimal = -99m;
    }
}

public class SettingsTradingShort : SettingsTradingBase
{
    public SettingsTradingShort()
    {
        Barometer15mBotMinimal = +0.5m;
        Barometer30mBotMinimal = +0.5m;
        Barometer01hBotMinimal = -0.3m;
        Barometer04hBotMinimal = +1.0m;
        Barometer24hBotMinimal = +99m;
    }
}

[Serializable]
public class CryptoDcaEntry
{
    public decimal Factor { get; set; }
    public decimal Percentage { get; set; }
}


[Serializable]
public class SettingsTrading
{
    // Is de BOT actief
    public bool Active { get; set; } = false;

    // TODO: Iedere exchange heeft 0 of meer key/secret's
    // (ze moeten ook nog ff versleuteld worden lijkt me)
    // Liefst in database zodat ze niet leesbaar in json staan
    // https://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    public string ApiPassPhrase { get; set; } = ""; // Kucoin


    //***************************
    // Account - Positie gerelateerd

    // De 3 account types zijn raar gekozen
    //public CryptoTradeAccountType TradeVia { get; set; } = CryptoTradeAccountType.PaperTrade;

    // Alles is functioneel in de bot, echter we simuleren of we aan het traden zijn
    public bool TradeViaPaperTrading { get; set; } = false;
    // Trade via exchange (instelling enkel omdat we nu keuze hebben)
    public bool TradeViaExchange { get; set; } = false;
    // Geen nieuwe posities openen (wel bijkopen voor openstaande posities)
    public bool DisableNewPositions { get; set; } = false;

    // =Overkill in de logging
    public bool LogCanceledOrders { get; set; } = true;


    //***************************
    // Slots
    //Maximaal aantal slots voor long en short
    public int SlotsMaximalLong { get; set; } = 1;
    public int SlotsMaximalShort { get; set; } = 1;


    //***************************
    // Instap condities
    public bool CheckIncreasingRsi { get; set; } = false;
    public bool CheckIncreasingMacd { get; set; } = false;    
    public bool CheckIncreasingStoch { get; set; } = false;


    //***************************
    // Buy
    // Wanneer wordt de order geplaatst
    public CryptoEntryOrProfitMethod BuyStepInMethod { get; set; } = CryptoEntryOrProfitMethod.AfterNextSignal;
    // De manier waarop de buy order geplaatst wordt
    public CryptoBuyOrderMethod BuyOrderMethod { get; set; } = CryptoBuyOrderMethod.MarketOrder;
    // Verwijder de order indien niet na zoveel minuten gevuld
    public int GlobalBuyRemoveTime { get; set; } = 5;
    //Het afwijkend percentage bij het kopen
    public decimal GlobalBuyVarying { get; set; } = -0.01m; // verlagen

    //***************************
    // Bijkopen (DCA)
    // Wanneer plaatsen we de DCA?
    public CryptoEntryOrProfitMethod DcaStepInMethod { get; set; } = CryptoEntryOrProfitMethod.FixedPercentage;
    // De manier waarop de buy order geplaatst wordt
    public CryptoBuyOrderMethod DcaOrderMethod { get; set; } = CryptoBuyOrderMethod.SignalPrice;

    

    // Tijd na een buy om niets te doen (om ladders te voorkomen)
    public int GlobalBuyCooldownTime { get; set; } = 30;

    //***************************
    // Sell
    // Het verkoop bedrag = buy bedrag * (100+profit / 100)
    public decimal ProfitPercentage { get; set; } = 1.01m;
    // De manier waarop de order geplaatst wordt
    public CryptoSellMethod SellMethod { get; set; } = CryptoSellMethod.FixedPercentage;
    // Zet een OCO zodra we in de winst zijn (kan het geen verlies trade meer worden, samen met tracing)
    //public bool LockProfits { get; set; } = false;

    //***************************
    // Stopp loss
    // Vanwege ontbreken stoplimit op Bybit afgesterd
    //public decimal GlobalStopPercentage { get; set; } = 0m;
    //public decimal GlobalStopLimitPercentage { get; set; } = 0m;


    //***************************
    // Perpetual / Futures
    // De buy en sell leverage (die zijn in alle gevallen gelijk)
    public decimal Leverage{ get; set; } = 1m;
    // Cross Of Isolated Margin trading
    public int CrossOrIsolated { get; set; } = 1;


    // Op welke intervallen, strategieën, trend, barometer willen we traden?
    public SettingsTextual Long { get; set; } = new();
    public SettingsTextual Short { get; set; } = new();

    public List<PauseTradingRule> PauseTradingRules { get; set; } = new();

    // De lijst met bijkopen
    public List<CryptoDcaEntry> DcaList { get; set; } = new();

    // Instap condities indien de "trend" positief is (up/down)
    //public List<string> TrendOn { get; set; } = new();

    //***************************
    // Hervatten van trading als er positieve signalen zijn (automatisch)
    // (misschien als meerdere munten weer omhoog gaan?)
    // TODO: Hier eens over nadenken..


    public SettingsTrading()
    {
        Long.Barometer.List.Add("1h", (-1.5m, 999m));
        Short.Barometer.List.Add("1h", (-999m, 1.5m));

        Long.IntervalTrend.List.Add("1h");
        Short.IntervalTrend.List.Add("1h");

        //Long.MarketTrend.List.Add((0m, 100m));
        //Short.MarketTrend.List.Add((-100m, 0));

        PauseTradingRules.Add(new PauseTradingRule()
        {
            Symbol = "BTCUSDT",
            Percentage = 1.5,
            Candles = 5,
            Interval = CryptoIntervalPeriod.interval2m,
            CoolDown = 20,
        });

        PauseTradingRules.Add(new PauseTradingRule()
        {
            Symbol = "BTCUSDT",
            Percentage = 2.5,
            Candles = 5,
            Interval = CryptoIntervalPeriod.interval5m,
            CoolDown = 10,
        });


        DcaList.Add(new CryptoDcaEntry()
        {
            Factor = 2,
            Percentage = 1.5m,
        });
        DcaList.Add(new CryptoDcaEntry()
        {
            Factor = 4,
            Percentage = 4.5m,
        });
    }

}

