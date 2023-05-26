using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Settings;


public enum BuyPriceMethod
{
    BidPrice,
    AskPrice,
    BidAndAskPriceAvg,
    Sma20,
    LowerBollingerband,
    MarketOrder
}

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
    // De manier waarop de order geplaatst wordt
    public BuyPriceMethod BuyMethod { get; set; } = BuyPriceMethod.BidPrice;
    // Verwijder de order indien niet na zoveel minuten gevuld
    public int GlobalBuyRemoveTime { get; set; } = 15;
    //Het afwijkend percentage bij het kopen
    public decimal GlobalBuyVarying { get; set; } = -0.1m; // aankoop verlagen

    //***************************
    // Bijkopen (DCA)
    public int DcaCount { get; set; } = 0; // niet bijkopen
    // Percentage bijkopen
    public decimal DcaPercentage { get; set; } = 2m;
    // Het DCA multifactor (normaal 1, meer bijkopen?)
    public decimal DcaFactor { get; set; } = 1; // Het aantal DCA's (loopt snel op!)
    // De manier waarop de order geplaatst wordt
    public BuyPriceMethod DcaMethod { get; set; } = BuyPriceMethod.MarketOrder;
    // Tijd na een buy om niets te doen (om ladders te voorkomen)
    public int GlobalBuyCooldownTime { get; set; } = 15;

    //***************************
    // Sell
    // Het verkoop bedrag = buy bedrag * (100+profit / 100)
    public decimal ProfitPercentage { get; set; } = 0.7m;
    // De manier waarop de order geplaatst wordt
    public BuyPriceMethod SellMethod { get; set; } = BuyPriceMethod.BidPrice;


    //***************************
    // Stopp loss
    public decimal GlobalStopPercentage { get; set; } = 4m;
    public decimal GlobalStopLimitPercentage { get; set; } = 5m;





    // Alles is functioneel in de bot, echter NIET instappen (enkel melden dat het zou instappen)
    public bool DoNotEnterTrade { get; set; } = false;
    // Alles is functioneel in de bot, echter we simuleren of we aan het traden zijn
    public bool TradeViaPaperTrading { get; set; } = false;
    // Trade via de webhook van Altrady (functioneel gehandicapt, ook diverse bugs)
    public bool TradeViaAltradyWebhook { get; set; } = false;
    // Trade via Binance (instelling enkel omdat we nu keuze hebben)
    public bool TradeViaBinance { get; set; } = false;

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

        Monitor.Strategy[TradeDirection.Long].Add("sbm1");
        Monitor.Strategy[TradeDirection.Long].Add("sbm2");
        Monitor.Strategy[TradeDirection.Long].Add("sbm3");

        PauseTradingRules.Add(new PauseTradingRule()
        {
            Symbol = "BTCUSDT",
            Percentage = 1.5,
            Candles = 5,
            Interval = CryptoIntervalPeriod.interval2m,
            CoolDown = 5,
        });

        PauseTradingRules.Add(new PauseTradingRule()
        {
            Symbol = "BTCUSDT",
            Percentage = 2.5,
            Candles = 5,
            Interval = CryptoIntervalPeriod.interval5m,
            CoolDown = 4,
        });
    }

}

