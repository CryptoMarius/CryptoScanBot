﻿using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Signal;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;


/// <summary>
/// Een position is 1 een samenvatting van 1 of meerdere orders
/// </summary>
[Table("Position")]
public class CryptoPosition
{
    [Key]
    public int Id { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public DateTime? CloseTime { get; set; }

    public int TradeAccountId { get; set; }
    [Computed]
    public required virtual CryptoAccount Account { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public required virtual CryptoExchange Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public required virtual CryptoSymbol Symbol { get; set; }

    public int? IntervalId { get; set; }
    [Computed]
    public required CryptoInterval? Interval { get; set; }

    public CryptoTradeSide Side { get; set; }
    [Computed]
    public string SideText { get { return Side.ToString().ToLower(); } }
    //switch { CryptoTradeSide.Long => "long", CryptoTradeSide.Short => "short", _ => "?", }; } }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Interval!.Name + " " + CreateTime.ToLocalTime() + " " + SideText + " " + StrategyText; } }

    public CryptoSignalStrategy Strategy { get; set; }
    [Computed]
    public string StrategyText { get { return SignalHelper.GetAlgorithm(Strategy); } }

    // Globale status van de positie (new, closed, wellicht andere enum?)
    public CryptoPositionStatus? Status { get; set; }

    public decimal Invested { get; set; }
    public decimal Returned { get; set; }

    // The commission in quote quantity
    public decimal Commission { get; set; }
    // The fee in base quantity (Bybit Spot) (0 if none)
    public decimal CommissionBase { get; set; }
    // The fee in quote quantity (Bybit Spot)
    public decimal CommissionQuote { get; set; }
    // Amount in orders
    public decimal Reserved { get; set; }
    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }

    public decimal Quantity { get; set; }
    // Expected dust because of fees and ticksizes  etc.
    public decimal RemainingDust { get; set; }
    // The Break Even Price
    public decimal BreakEvenPrice { get; set; }

    // Hulpmiddelen voor statistiek en dca (niet noodzakelijk)
    public decimal? EntryPrice { get; set; }
    public decimal? EntryAmount { get; set; }
    public decimal? ProfitPrice { get; set; }

    // Een experiment (die weg kan, we zetten er nu even de naam van de munt in, handig)
    public string Data { get; set; } = "";

    // Is de Parts.Count (met een active DCA)
    public int PartCount { get; set; }
    public bool ActiveDca { get; set; }

    // Zou computed kunnen, maar voor de zekerheid in de database
    public bool Reposition { get; set; }

    [Computed]
    public DateTime? DelayUntil { get; set; }

    [Computed]
    public bool IsChanged { get; set; }

    [Computed]
    // todo -> rename to PartList!
    public SortedList<int, CryptoPositionPart> PartList { get; set; } = [];

    [Computed]
    // Orders die uitstaan via de parts/steps
    // TODO -> rename to StepList!
    public SortedList<string, CryptoPositionStep> StepOrderList { get; set; } = [];

    [Computed]
    public SemaphoreSlim Semaphore { get; set; } = new(1);
    [Computed]
    public bool ForceCheckPosition { get; set; } = false;



    [Computed]
    public bool HasOrdersAndTradesLoaded { get; set; } = false;

    [Computed]
    // Exchange orders (Key=OrderId, value=CryptoOrder)
    public CryptoOrderList OrderList { get; set; } = [];
 
    [Computed]
    // Exchange trades (Key=TradeId, value=CryptoTrade)
    public CryptoTradeList TradeList { get; set; } = [];



    /// --------------------------------------------------------------
    /// added from the signal...
    /// --------------------------------------------------------------
    public DateTime SignalEventTime { get; set; }
    public decimal SignalPrice { get; set; }
    public decimal SignalVolume { get; set; }

    public double Last24HoursChange { get; set; }
    public double Last24HoursEffective { get; set; }
    public double Last10DaysEffective { get; set; }

    public float TrendPercentage { get; set; }
    public CryptoTrendIndicator TrendIndicator { get; set; }

    // Stochastic waarden
    public double? StochOscillator { get; set; } // Stochastic oscillator %K
    public double? StochSignal { get; set; }  // Stochastic oscillator %D

    // Bollinger Bands
    public double? BollingerBandsUpperBand { get; set; }
    public double? BollingerBandsLowerBand { get; set; }
    public double? BollingerBandsPercentage { get; set; }

    // PSAR waarden
    public double? PSar { get; set; }

    //public double? KeltnerUpperBand { get; set; }
    //public double? KeltnerLowerBand { get; set; }

    // RSI waarden
    public double? Rsi { get; set; }
    //public double? SlopeRsi { get; set; }

    public int LuxIndicator5m { get; set; }

    //uitgezet
    ////public double? Ema8 { get; set; }
    //public double? Ema20 { get; set; }
    //public double? Ema50 { get; set; }
    ////public double? Ema100 { get; set; }
    //public double? Ema200 { get; set; }
    //public double? SlopeEma20 { get; set; }
    //public double? SlopeEma50 { get; set; }

    // SMA waarden
    //public double? Sma8 { get; set; }
    public double? Sma20 { get; set; }
    public double? Sma50 { get; set; }
    //public double? Sma100 { get; set; }
    public double? Sma200 { get; set; }
    //public double? SlopeSma20 { get; set; } uitgezet
    //public double? SlopeSma50 { get; set; } uitgezet


    // Wellicht introduceren en weghalen uit de "Alarm"?
    public int CandlesWithZeroVolume { get; set; } // Candles zonder volume
    public int CandlesWithFlatPrice { get; set; } // De zogenaamde platte candles
    public int AboveBollingerBandsSma { get; set; } // Aantal candles die boven de BB.Sma uitkomen
    public int AboveBollingerBandsUpper { get; set; } // Aantal candles die boven de BB.Upper uitkomen

    // Een aantal trendindicatoren
    public CryptoTrendIndicator Trend15m { get; set; }
    public CryptoTrendIndicator Trend30m { get; set; }
    public CryptoTrendIndicator Trend1h { get; set; }
    public CryptoTrendIndicator Trend4h { get; set; }
    public CryptoTrendIndicator Trend12h { get; set; }
}


public static class CryptoPositionHelper
{
    /// <summary>
    /// Netto winst (als je nu zou verkopen)
    /// </summary>
    public static decimal CurrentProfit(this CryptoPosition position)
    {
        if (position.Status == CryptoPositionStatus.Ready)
            return position.Profit;

        if (!position.Symbol.LastPrice.HasValue)
            return 0m;
        else
        {
            decimal plannedValue = position.Quantity * position.BreakEvenPrice; // + position.RemainingDust ????
            decimal currentValue = position.Quantity * position.Symbol.LastPrice.Value;

            if (position.Side == CryptoTradeSide.Long)
                return currentValue - plannedValue;
            else
                return plannedValue - currentValue;
        }
    }

    public static string PartCountText(this CryptoPosition position, bool isOpenPosition = true)
    {
        int partCount = position.PartCount + 1; // entry geld ook als 1
        //if (position.ActiveDca)
        //    partCount--;
        // En we willen de openstaande part niet zien totdat deze echt gevuld is
        string text = partCount.ToString();
        // + ten teken dat er een openstaande DCA klaar staat (wellicht ook nog dat ie manual is)
        if (position.ActiveDca && isOpenPosition)
            text += "+";
        return text;
    }

    /// <summary>
    /// Netto waarde (als je nu zou verkopen)
    /// </summary>
    public static decimal CurrentValue(this CryptoPosition position)
    {
        if (position.Status == CryptoPositionStatus.Ready)
            return 0; // position.Profit; die hebben we niet meer..

        return position.Invested - position.Returned + position.CurrentProfit();
    }

    /// <summary>
    /// Winst percentage (als je nu zou verkopen)
    /// </summary>
    public static decimal CurrentProfitPercentage(this CryptoPosition position)
    {
        if (position.Status == CryptoPositionStatus.Ready)
            return position.Percentage;

        decimal total = position.Invested - position.Returned;
        if (total == 0)
            return 0m;
        else
        {
            if (position.Invested != 0)
                return 100 * position.CurrentProfit() / position.Invested; // total; Met de invested is het de netpnl% van altrady
            else return 0;
        }
    }

    public static decimal CurrentBreakEvenPercentage(this CryptoPosition position)
    {
        if (position.Status == CryptoPositionStatus.Ready)
            return position.Percentage;

        if (!position.Symbol.LastPrice.HasValue)
            return 0m;

        if (position.BreakEvenPrice == 0 || position.Symbol.LastPrice.Value == 0)
            return 0;

        if (position.Side == CryptoTradeSide.Long)
            return 100 - 100 * position.BreakEvenPrice / position.Symbol.LastPrice.Value;
        else
            return 100 - 100 * position.Symbol.LastPrice.Value / position.BreakEvenPrice;
    }

    public static TimeSpan Duration(this CryptoPosition position)
    {
        TimeSpan span;
        if (position.CloseTime.HasValue)
            span = (DateTime)position.CloseTime - position.CreateTime;
        else
            span = DateTime.UtcNow - position.CreateTime;
        return span;
    }


    public static string DurationText(this CryptoPosition position)
    {
        TimeSpan span = position.Duration();

        string text = "";
        if (span.Days > 0)
            text += $"{span.Days}d";
        if (span.Hours > 0)
            text += $" {span.Hours}h";
        if (span.Minutes > 0)
            text += $" {span.Minutes}m";
        //if (span.Seconds > 0)
        //    text += $" {span.Seconds}s";
        return text.Trim();
    }


    public static CryptoOrderSide GetEntryOrderSide(this CryptoPosition position)
    {
        if (position.Side == CryptoTradeSide.Long)
            return CryptoOrderSide.Buy;
        else
            return CryptoOrderSide.Sell;
    }


    public static CryptoOrderSide GetTakeProfitOrderSide(this CryptoPosition position)
    {
        if (position.Side == CryptoTradeSide.Long)
            return CryptoOrderSide.Sell;
        else
            return CryptoOrderSide.Buy;
    }

}
