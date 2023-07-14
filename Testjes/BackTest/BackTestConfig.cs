using CryptoSbmScanner.Enums;

namespace CryptoSbmScanner.BackTest;

public enum EmulatorResult
{
    Waiting,
    Win,
    Lost,
    Timeout
}

public enum BuyPriceStrategy
{
    MarketOrder, // 0
    CandleLowest, // 1
    CandleAverage, // 2
    CandleHighest, // 3
    CandleClose, // 4
    BollingerBands, // 5 onderste band
    Experiment
}

public enum DcaMethod
{
    Absolute, // 0 bijkopen ten opzichte van de laatste koop opdracht
    Relative, // 1 bijkopen ten opzichte van de break-even positie
    ViaPrice, // 2 nieuw (een vergissing die goed uitpakt?)
    ViaLastPrice // 3 Omdat de laatste buyprice naar beneden gaat vanwege trailing
                 // ViaSbmMethod // nieuw de Maurice methode
}

public enum TakeProfitMethod
{
    FixedProfit, // 0
    BollingerBandSma, // 1
    BollingerBandUpper // 2
}

public class CryptoBackConfig
{
    // flow - select
    public bool ReleaseCandles = false;
    public string SymbolFilter = "LDO";
    public string QuoteMarket = "BUSD";
    public CryptoIntervalPeriod IntervalPeriod = CryptoIntervalPeriod.interval1m;
    public DateTime DateStart = DateTime.SpecifyKind(new DateTime(2021, 12, 01, 0, 0, 0), DateTimeKind.Utc);
    public DateTime DateEinde = DateTime.SpecifyKind(new DateTime(2022, 02, 01, 0, 0, 0), DateTimeKind.Utc);

    // flow - filter & display
    public decimal UsdtValue = 25000;
    public decimal VolumeLimit = 200000;
    public decimal PriceLimit = 0.000002m;
    public bool LogIncludeVolume = false;

    // filter signals
    public int CandlesWithFlatPrice = 15;
    public int CandlesWithZeroVolume = 15;
    public decimal AboveBollingerBandsSma = 2;
    public decimal AboveBollingerBandsUpper = 1;
    public decimal MinPercentagePriceTickSize = 0.2m;
    //public int UseCalculateExtraCandles = 30;

    // Take Profit
    public TakeProfitMethod TakeProfitMethod = TakeProfitMethod.BollingerBandSma;
    public int TakeProfitMethodCorrectionTicks = 0;

    public decimal TakeProfitMethodPercentage1 = 0.90m;
    public decimal TakeProfitMethodPercentage2 = 1.10m;
    public decimal TakeProfitMethodPercentage3 = 0m;
    public decimal TakeProfitMethodPercentage4 = 0m;

    public decimal TakeProfitMethodQuantityPercentage1 = 75m;
    public decimal TakeProfitMethodQuantityPercentage2 = 25;
    public decimal TakeProfitMethodQuantityPercentage3 = 25m;
    public decimal TakeProfitMethodQuantityPercentage4 = 00m;


    public decimal StoplossPercentage = 3.5m;

    public int AmountOfDca = 1;
    public decimal DcaFactor = 1.0m;
    public decimal BuyAmount = 0.05m; // Het aantal BTC per trade, zoals Zignally
    public DcaMethod DcaMethod = DcaMethod.Absolute;

    public bool UseProfitTrailing = false;
    public decimal UseSellTrailingPercentage = 2.5m;

    public bool UseSellTracing = false;
    public int UseSellTracingCandles = 4;
    public int UseSellTracingPSarCandles = 2;
    public bool UseSellTracingWithPSar = false;

    public bool UseBuyTracing = false;
    public int UseBuyTracingCandles = 4;
    public int UseBuyTracingPSarCandles = 2;
    public bool UseBuyTracingWithPSar = false;
    public bool UseBuyTracingForFirstOrder = false;

    public decimal WaitForStochOscillatorAbove = 30m;
    public decimal WaitForStochSignalAbove = 19m;

    public int DelayCandles = 0;
    public int WaitCandles = 10;
    //public int StepInTimeOut = 20;
    public BuyPriceStrategy BuyPriceStrategy = BuyPriceStrategy.CandleLowest;
    public decimal BuyPriceLower = 0.5m;

    public decimal BarometerMinimal = 99m;
    public decimal MovementMaximal = 0m;
}