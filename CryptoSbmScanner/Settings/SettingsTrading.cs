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


[Serializable]
public class SettingsTrading
{
    // Is de BOT actief
    public bool Active { get; set; } = false;
    public bool DisableNewPositions { get; set; } = false;

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
    // Barometer
    // De bot maakt geen nieuwe posities als de barometer onder een van deze getallen staat
    public decimal Barometer15mBotMinimal { get; set; } = -0.5m;
    public decimal Barometer30mBotMinimal { get; set; } = -0.5m;
    public decimal Barometer01hBotMinimal { get; set; } = 0.1m;
    public decimal Barometer04hBotMinimal { get; set; } = -0.5m;
    public decimal Barometer24hBotMinimal { get; set; } = -99m;


    //***************************
    // Buy
    // Wanneer wordt de order geplaatst
    public CryptoBuyStepInMethod BuyStepInMethod { get; set; } = CryptoBuyStepInMethod.Immediately;
    // De manier waarop de buy order geplaatst wordt
    public CryptoBuyOrderMethod BuyOrderMethod { get; set; } = CryptoBuyOrderMethod.BidPrice;
    // Verwijder de order indien niet na zoveel minuten gevuld
    public int GlobalBuyRemoveTime { get; set; } = 15;
    //Het afwijkend percentage bij het kopen
    public decimal GlobalBuyVarying { get; set; } = -0.1m; // aankoop verlagen

    //***************************
    // Bijkopen (DCA)
    // Wanneer plaatsen we de DCA?
    public CryptoBuyStepInMethod DcaStepInMethod { get; set; } = CryptoBuyStepInMethod.FixedPercentage;
    // De manier waarop de buy order geplaatst wordt
    public CryptoBuyOrderMethod DcaOrderMethod { get; set; } = CryptoBuyOrderMethod.MarketOrder;
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
    public bool LockProfits { get; set; } = false;
    public decimal DynamicTpPercentage { get; set; } = 0.6m;

    //***************************
    // Stopp loss
    public decimal GlobalStopPercentage { get; set; } = 0m;
    public decimal GlobalStopLimitPercentage { get; set; } = 0m;





    // Alles is functioneel in de bot, echter we simuleren of we aan het traden zijn
    public bool TradeViaPaperTrading { get; set; } = false;
    // Trade via exchange (instelling enkel omdat we nu keuze hebben)
    public bool TradeViaExchange { get; set; } = false;

    // Op welke intervallen en strategieën willen we traden?
    public IntervalAndStrategyConfig Monitor { get; set; } = new();



    //***************************
    // Pauseren van trading als een of meerdere munten (sterk) bewegen
    public string PauseTradingText { get; set; }
    public DateTime? PauseTradingUntil { get; set; }
    public List<PauseTradingRule> PauseTradingRules { get; set; } = new();


    //***************************
    // Hervatten van trading als er positieve signalen zijn (automatisch)
    // (misschien als meerdere munten weer omhoog gaan?)
    // TODO: Hier eens over nadenken..


    public SettingsTrading()
    {
        Monitor.Interval.Add("1m");
        Monitor.Interval.Add("2m");

        Monitor.Strategy[CryptoOrderSide.Buy].Add("sbm1");
        Monitor.Strategy[CryptoOrderSide.Buy].Add("sbm2");
        Monitor.Strategy[CryptoOrderSide.Buy].Add("sbm3");

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

