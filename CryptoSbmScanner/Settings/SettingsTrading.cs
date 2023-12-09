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
public class SettingsTrading
{
    // Is de BOT actief
    public bool Active { get; set; } = false;

    // TODO: Iedere exchange heeft 0 of meer key/secret's
    // (ze moeten ook nog ff versleuteld worden lijkt me)
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    public string ApiPassPhrase { get; set; } = "";


    //***************************
    // Account - Positie gerelateerd
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
    //Maximaal aantal slots op de exchange
    public int SlotsMaximalExchange { get; set; } = 1;
    //Maximaal aantal slots per munt/asset
    public int SlotsMaximalSymbol { get; set; } = 1;
    //Maximaal aantal slots op de *USDT, *BTC, *ETH, enzovoort
    //public int SlotsMaximalQuote { get; set; } = 1; dubbelop met de basismunten!
    //Maximaal aantal slots op de AVE*, ADA*, enzovoort
    public int SlotsMaximalBase { get; set; } = 1;


    //***************************
    // Instap condities
    public bool CheckIncreasingRsi { get; set; } = false;
    public bool CheckIncreasingMacd { get; set; } = false;    
    public bool CheckIncreasingStoch { get; set; } = false;


    //***************************
    // Buy
    // Wanneer wordt de order geplaatst
    public CryptoStepInMethod BuyStepInMethod { get; set; } = CryptoStepInMethod.AfterNextSignal;
    // De manier waarop de buy order geplaatst wordt
    public CryptoBuyOrderMethod BuyOrderMethod { get; set; } = CryptoBuyOrderMethod.BidPrice;
    // Verwijder de order indien niet na zoveel minuten gevuld
    public int GlobalBuyRemoveTime { get; set; } = 15;
    //Het afwijkend percentage bij het kopen
    public decimal GlobalBuyVarying { get; set; } = -0.1m; // aankoop verlagen

    //***************************
    // Bijkopen (DCA)
    // Wanneer plaatsen we de DCA?
    public CryptoStepInMethod DcaStepInMethod { get; set; } = CryptoStepInMethod.FixedPercentage;
    // De manier waarop de buy order geplaatst wordt
    public CryptoBuyOrderMethod DcaOrderMethod { get; set; } = CryptoBuyOrderMethod.SignalPrice;
    //public bool DcaOrderMethod2 { get; set; } = CryptoBuyOrderMethod.MarketOrder;
    // Hoe vaak mogen we bijkopen
    public int DcaCount { get; set; } = 0; // niet bijkopen
    // Percentage bijkopen
    public decimal DcaPercentage { get; set; } = 2m;
    // Het DCA multifactor (normaal 1, meer bijkopen?)
    public decimal DcaFactor { get; set; } = 1; // Het bedrag met x DCA's loopt snel op!


    

    // Tijd na een buy om niets te doen (om ladders te voorkomen)
    public int GlobalBuyCooldownTime { get; set; } = 15;

    //***************************
    // Sell
    // Het verkoop bedrag = buy bedrag * (100+profit / 100)
    public decimal ProfitPercentage { get; set; } = 1m;
    // De manier waarop de order geplaatst wordt
    public CryptoSellMethod SellMethod { get; set; } = CryptoSellMethod.FixedPercentage;
    // Zet een OCO zodra we in de winst zijn (kan het geen verlies trade meer worden, samen met tracing)
    //public bool LockProfits { get; set; } = false;

    //***************************
    // Stopp loss
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

    // Instap condities indien de "trend" positief is (up/down)
    //public List<string> TrendOn { get; set; } = new();

    //***************************
    // Hervatten van trading als er positieve signalen zijn (automatisch)
    // (misschien als meerdere munten weer omhoog gaan?)
    // TODO: Hier eens over nadenken..


    public SettingsTrading()
    {
        PauseTradingRules.Add(new PauseTradingRule()
        {
            Symbol = "BTCUSDT",
            Percentage = 1.5,
            Candles = 5,
            Interval = CryptoIntervalPeriod.interval2m,
            CoolDown = 10,
        });

        PauseTradingRules.Add(new PauseTradingRule()
        {
            Symbol = "BTCUSDT",
            Percentage = 2.5,
            Candles = 5,
            Interval = CryptoIntervalPeriod.interval5m,
            CoolDown = 8,
        });
    }

}

