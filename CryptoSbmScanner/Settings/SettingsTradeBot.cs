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
public class SettingsTradeBot
{
    // Is de BOT actief
    public bool Active { get; set; } = false;

    //Buy Amounts, percentage
    //Het initiele koopbedrag (fixed USDT overigens!)
    //public decimal GlobalBuyAmount { get; set; }
    //Het initiele kooppercentage
    //public decimal GlobalBuyPercentage { get; set; }
    //Tijd waarna een niet gevulde buy order wordt weggehaald, (nog te realiseren, dwz nog niet werkend gezien)
    public int GlobalBuyRemoveTime { get; set; } = 15;
    //Tijd na een buy om helemaal niets te doen (om ladders te voorkomen), (nog te realiseren)
    public int GlobalBuyCooldownTime { get; set; } = 15;
    //Het afwijkend percentage bij het kopen
    public decimal GlobalBuyVarying { get; set; } = -0.1m; // aankoop verlagen

    // De OCO instellingen
    public decimal GlobalStopPercentage { get; set; } = 4m;
    public decimal GlobalStopLimitPercentage { get; set; } = 5m;

    //Het verkoop bedrag = buy bedrag * (100+profit / 100)
    public decimal ProfitPercentage { get; set; } = 0.7m;

    //Het DCA pecentage bijkopen
    public decimal DcaPercentage { get; set; } = 2m;
    //Het DCA multifactor (normaal 1, meer bijkopen?)
    public decimal DcaFactor { get; set; } = 1; // 1 op 1 (loopt snel op!)
                                                //Het aantal DCA's
    public int DcaCount { get; set; } = 2; // 2x bijkopen

    //Maximaal aantal slots op de exchange
    public int SlotsMaximalExchange { get; set; } = 1;
    //Maximaal aantal slots per munt/asset
    public int SlotsMaximalSymbol { get; set; } = 1;
    //Maximaal aantal slots op de *USDT, *BTC, *ETH, enzovoort
    //public int SlotsMaximalQuote { get; set; } = 1; dubbelop met de basismunten!
    //Maximaal aantal slots op de AVE*, ADA*, enzovoort
    public int SlotsMaximalBase { get; set; } = 1;

    //De bot maakt geen nieuwe posities als de barometer onder een van deze getallen staat
    public decimal Barometer15mBotMinimal { get; set; } = -0.5m;
    public decimal Barometer30mBotMinimal { get; set; } = -0.5m;
    public decimal Barometer01hBotMinimal { get; set; } = 0.1m;
    public decimal Barometer04hBotMinimal { get; set; } = -0.5m;
    public decimal Barometer24hBotMinimal { get; set; } = -99m;

    public BuyPriceMethod BuyPriceMethod { get; set; } = BuyPriceMethod.BidPrice;


    // Alles is functioneel in de bot, echter NIET instappen (enkel melden dat het zou instappen)
    public bool DoNotEnterTrade { get; set; } = false;


    // Op welke intervallen willen we traden?
    public bool[] TradeOnInterval { get; set; } = new bool[Enum.GetNames(typeof(CryptoIntervalPeriod)).Length];
    // Op welke strategieën willen we traden?
    public bool[] TradeOnStrategy { get; set; } = new bool[Enum.GetNames(typeof(SignalStrategy)).Length];


    public SettingsTradeBot()
    {
        TradeOnInterval[(int)CryptoIntervalPeriod.interval1m] = true;
        TradeOnInterval[(int)CryptoIntervalPeriod.interval2m] = true;
        TradeOnInterval[(int)CryptoIntervalPeriod.interval3m] = true;
    }

}

