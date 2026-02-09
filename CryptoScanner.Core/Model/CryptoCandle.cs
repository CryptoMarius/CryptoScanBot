using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal;

using Dapper.Contrib.Extensions;

using Skender.Stock.Indicators;

using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Model;


// new idea:
// We specify candle.open etc in uint(4) / ticksize instead of decimal(8).
// We could spefify the rest in an offset of the open
// this saves 4 * 8 - 4 * 4 = 16 bytes per record (might even use smallint for offset?)
//
// Additional:
// Specify the opentime in uint(4) instead of long(8)
// save ~20 bytes per candle * 4.000.000 candles = 80 Mb (+more because of dictionary key's)
// but its a lot of work..

//public struct CryptoCandle
//{
//    public uint OpenTime;   // minutes since Epoch (2010)
//    public int Open;
//    public int High;
//    public int Low;
//    public int Close;
//    public int Volume;
//}

public class CryptoCandle : IQuote
//[StructLayout(LayoutKind.Sequential, Pack = 1)]
//public struct CryptoCandle : IQuote
{
    public long OpenTime { get; set; } // a long is 64 bit / 8 bytes, we can reduce this (uint, count only the minutes, seconds not needed)
    public decimal Open { get; set; } // a decimal is an amazing 16 bytes
    public decimal High { get; set; } // Could trick this in amount of ticks away from open +/-, problem is the non fixed tick size..
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; } // float or double will suffice (but with rounding errors)

    // Idea, we store it as uint together with the factor, this saves 50% memory
    //public uint OpenStorage { get; set; }
    //public uint HighStorage { get; set; }
    //public uint LowStorage { get; set; }
    //public uint CloseStorage { get; set; }

    //// decimal = 16 bytes, long = 8, uint = 4
    //// 4*16 - 3*4 = 64 - 12 = 52 bytes per candle, is that wurth the effort??
    //[Computed]
    //public uint PriceFactor { get; set; }
    //[Computed]
    //public decimal OpenDecimal { get { return (long)OpenStorage / PriceFactor; } set { OpenStorage = (uint)(value * PriceFactor); } }
    //[Computed]
    //public decimal HighDecimal { get { return (long)HighStorage / PriceFactor; } set { HighStorage = (uint)(value * PriceFactor); } }
    //[Computed]
    //public decimal LowDecimal { get { return (long)LowStorage / PriceFactor; } set { LowStorage = (uint)(value * PriceFactor); } }
    //[Computed]
    //public decimal CloseDecimal { get { return (long)CloseStorage / PriceFactor; } set { CloseStorage = (uint)(value * PriceFactor); } }


    public DateTime Date { get { return CandleTools.GetUnixDate(OpenTime); } }
    public DateTime DateLocal { get { return CandleTools.GetUnixDate(OpenTime).ToLocalTime(); } }
    public CandleIndicatorData? CandleData { get; set; }
}

//
// For a future Helperclass? (StringStream?)
//
//public void DumpSymbol()
//{
//    //Ter debug van een hardnekig probleem met het tonen van de signal
//    var csv = new StringBuilder();
//    var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", "OpenTime", "IntervalId", "Open", "High", "Low", "Close", "Volume");
//    csv.AppendLine(newLine);

//    Monitor.Enter(History);
//    try
//    {
//        for (int i = 0; i < History.Count; i++)
//        {
//            CryptoCandle candle = History[i];

//            newLine = string.Format("{0};{1};{2};{3};{4};{5};{6}",
//            candle.Time.ToString(),
//            candle.IntervalId.ToString(),
//            candle.Open.ToString(),
//            candle.High.ToString(),
//            candle.Low.ToString(),
//            candle.Close.ToString(),
//            candle.Volume.ToString());

//            csv.AppendLine(newLine);
//        }
//    }
//    finally
//    {
//        Monitor.Exit(History);
//    }
//    string filename = System.IO.Path.GetDirectoryName((System.Reflection.Assembly.GetEntryAssembly().Location));
//    filename = filename + @"\data\" + Symbol.Exchange.Name + @"\Candles\" + Interval.Name + @"\";
//    System.IO.Directory.CreateDirectory(filename);
//    System.IO.File.WriteAllText(filename + Symbol.Name + "-" + Interval.Name + ".csv", csv.ToString());
//}


// https://grok.com/share/c2hhcmQtNA_68613833-89da-4e6b-8489-2f903eb55ab4

public struct CryptoCandleIdea : IQuote
{
    // Shared per array/asset (niet per candle)
    public static long PriceScale { get; set; } = 100_000_000; // Bijv. voor 8 decimals
    public static long VolumeScale { get; set; } = 1_000_000; // Afhankelijk van volume precisie

    // Interne opslag (deltas, scaled)
    public uint OpenTimeOffset { get; set; } // Uint offset in minutes vanaf base time
    public long OpenStorage { get; set; } // Absoluut voor eerste, anders delta scaled
    public int HighDelta { get; set; } // Scaled delta from Open
    public int LowDelta { get; set; } // Scaled delta from Open (positief)
    public int CloseDelta { get; set; } // Scaled delta from Open
    public double VolumeStorage { get; set; } // Scaled, uint want positief

    // Constructor/logic om te encoden (in een manager class)
    // Bijv. bij vullen: if (index == 0) OpenStorage = (long)(open * PriceScale); else OpenStorage = (long)((open - prev.Close) * PriceScale);

    // Getters (vereist decoding met prev, dus beter in een array-manager)
    // Voor simplicity: assume een ArrayCandleManager die absolutes cached of computed
    public decimal Open => OpenStorage / (decimal)PriceScale;
    public decimal High => Open + HighDelta / (decimal)PriceScale;
    public decimal Low => Open - LowDelta / (decimal)PriceScale;
    public decimal Close => Open + CloseDelta / (decimal)PriceScale;
    public decimal Volume { readonly get => (decimal)VolumeStorage; set { VolumeStorage = (double)value; } }

    public DateTime Date {get; set;} //=> BaseDate.AddMinutes(OpenTimeOffset); // BaseDate static
    // ... andere properties
}

//public class CandleArray
//{
//    private CryptoCandle[] _candles;
//    private DateTime _baseDate;
//    private long _baseTime;

//    public void AddCandle(decimal open, decimal high, /*...*/, long openTime)
//    {
//        // Bereken deltas t.o.v. laatste, scale, store
//        // Voor time: OpenTimeOffset = (uint)((openTime - _baseTime) / 60_000); // Minutes
//    }

//    public decimal GetOpen(int index)
//    {
//        // Cumuleer deltas vanaf 0 tot index (of gebruik checkpoints elke 100)
//        decimal current = _candles[0].Open;
//        for (int i = 1; i <= index; i++)
//        {
//            current += _candles[i].OpenStorage / (decimal)CryptoCandle.PriceScale; // Delta
//        }
//        return current;
//    }
//    // Soortgelijk voor anderen; cache voor speed
//}

public static class TimeConverter
{
    public static readonly DateTime Epoch =
        new(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly long EpochUnixMs =
        new DateTimeOffset(Epoch).ToUnixTimeMilliseconds();

    private static readonly long EpochUnixSec =
        new DateTimeOffset(Epoch).ToUnixTimeSeconds();

    private const int SecondsPerMinute = 60;
    private const int MillisPerMinute = 60_000;

    // ----------------------------
    // unix milliseconds -> uint minutes
    // ----------------------------
    public static uint FromUnixMilliseconds(long unixMs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(unixMs, EpochUnixMs);

        long minutes = (unixMs - EpochUnixMs) / MillisPerMinute;
        return (uint)minutes;
    }

    // ----------------------------
    // unix seconds -> uint minutes
    // ----------------------------
    public static uint FromUnixSeconds(long unixSec)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(unixSec, EpochUnixSec);

        long minutes = (unixSec - EpochUnixSec) / SecondsPerMinute;
        return (uint)minutes;
    }

    // ----------------------------
    // uint minutes -> DateTime UTC
    // ----------------------------
    public static DateTime ToDateTime(uint minutes)
        => Epoch.AddMinutes(minutes);

    // ----------------------------
    // uint minutes -> unix milliseconds
    // ----------------------------
    public static long ToUnixMilliseconds(uint minutes)
        => EpochUnixMs + ((long)minutes * MillisPerMinute);

    // ----------------------------
    // uint minutes -> unix seconds
    // ----------------------------
    public static long ToUnixSeconds(uint minutes)
        => EpochUnixSec + ((long)minutes * SecondsPerMinute);
}
