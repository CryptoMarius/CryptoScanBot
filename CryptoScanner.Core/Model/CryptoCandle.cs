using Skender.Stock.Indicators;

using System.Diagnostics;
using System.Runtime.InteropServices;

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

//int 	-2,147,483,648 to 2,147,483,647 Signed          32-bit integer 	System.Int32                    4 bytes
//uint 	0 to 4,294,967,295 Unsigned                     32-bit integer 	System.UInt32                   4 bytes
//long 	-9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 Signed 64-bit integer 	System.Int64    8 bytes
//float 	±1.5 x 10−45 to ±3.4 x 1038 	~6-9 digits 	4 bytes 	System.Single                   4 bytes
//double 	±5.0 × 10−324 to ±1.7 × 10308 	~15-17 digits 	8 bytes 	System.Double                   8 bytes
//decimal 	±1.0 x 10-28 to ±7.9228 x 1028 	28-29 digits 	16 bytes System.Decimal                     16 bytes

//candle
// opentime als uint   =   4
// 4 decimals * 16      = 64
// volume as double      = 8
//---------------------------
// Total                = 88 bytes per candle, for 4.000.000 candles = 352 Mb (without dictionary keys etc)

// Reduce the long to a unit (minutes from 01-01-2020)
// Saves 4 bytes a candle, for 4.000.000 candles = 16 Mb (without dictionary keys etc)
// Because of the dictionary keys it saves already 32 Mb.. not really big (and a lot of work)


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CryptoCandle : IQuote
{
    public CandleTime OpenTime { get; set; } // uint
    //public decimal Open { get; set; } // a decimal is an amazing 16 bytes
    //public decimal High { get; set; } // Could trick this in amount of ticks away from open +/-, problem is the non fixed tick size..
    //public decimal Low { get; set; }
    //public decimal Close { get; set; }

    // works fine...
    // Storage fields (internal, in satoshi's)
    //private const decimal SatoshiMultiplier = 100_000_000m;
    //private long _openSatoshi;
    //public decimal Open { get => _openSatoshi / SatoshiMultiplier; set => _openSatoshi = (long)(value * SatoshiMultiplier);}
    //private long _highSatoshi;
    //public decimal High { get => _highSatoshi / SatoshiMultiplier; set => _highSatoshi = (long)(value * SatoshiMultiplier);}
    //private long _lowSatoshi;
    //public decimal Low { get => _lowSatoshi / SatoshiMultiplier; set => _lowSatoshi = (long)(value * SatoshiMultiplier); }
    //private long _closeSatoshi;
    //public decimal Close { get => _closeSatoshi / SatoshiMultiplier; set => _closeSatoshi = (long)(value * SatoshiMultiplier); }

    // Properties
    public byte TickDecimals;                 // 1 byte (aantal decimalen in tickSize)
    // Pre-calculated tick sizes (0-8 decimals), less costly then Math.Pow()
    private static readonly decimal[] TickSizeLookup =
    {
        1.0m,           // 0 decimals
        0.1m,           // 1 decimal
        0.01m,          // 2 decimals
        0.001m,         // 3 decimals
        0.0001m,        // 4 decimals
        0.00001m,       // 5 decimals
        0.000001m,      // 6 decimals
        0.0000001m,     // 7 decimals
        0.00000001m     // 8 decimals
    };
    //private decimal TickSize => 1m / (decimal)Math.Pow(10, TickDecimals);
    private decimal TickSize => TickSizeLookup[TickDecimals];
    private int _openTicks;                    // 4 bytes
    public decimal Open { get => _openTicks * TickSize; set => _openTicks = (int)Math.Round(value / TickSize); }
    private int _highTicks;                    // 4 bytes
    public decimal High { get => _highTicks * TickSize; set => _highTicks = (int)Math.Round(value / TickSize); }
    private int _lowTicks;                     // 4 bytes
    public decimal Low { get => _lowTicks * TickSize; set => _lowTicks = (int)Math.Round(value / TickSize); }
    private int _closeTicks;                   // 4 bytes
    public decimal Close { get => _closeTicks * TickSize; set => _closeTicks = (int)Math.Round(value / TickSize); }

    private double _volume;
    public decimal Volume { get { return (decimal)_volume; } set { _volume = (double)value; } } // float or double will suffice (but with rounding errors)

    public DateTime Date { get { return OpenTime.ToDateTime(); } }
    public DateTime DateLocal { get { return OpenTime.ToDateTime().ToLocalTime(); } }

    // Better: Direct calculation
    public static byte CalculateDecimalsFromTickSize2(decimal tickSize)
    {
        // tickSize = 1 / (10^decimals)
        // decimals = -log10(tickSize)

        if (tickSize <= 0)
            throw new ArgumentException("TickSize must be positive");
        byte decimals = (byte)Math.Round(-Math.Log10((double)tickSize));
        return decimals;
    }

    public void LoadVersion3(BinaryReader reader)
    {
        OpenTime = new CandleTime(reader.ReadUInt32());
        _openTicks = reader.ReadInt32();
        _highTicks = reader.ReadInt32();
        _lowTicks = reader.ReadInt32();
        _closeTicks = reader.ReadInt32();
        _volume = reader.ReadDouble();
    }

    public readonly void SaveVersion3(BinaryWriter writer)
    {
        writer.Write(OpenTime.Minutes);
        writer.Write(_openTicks);
        writer.Write(_highTicks);
        writer.Write(_lowTicks);
        writer.Write(_closeTicks);
        writer.Write(_volume);
    }
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

//public struct CryptoCandleIdea : IQuote
//{
//    // Shared per array/asset (niet per candle)
//    public static long PriceScale { get; set; } = 100_000_000; // Bijv. voor 8 decimals
//    public static long VolumeScale { get; set; } = 1_000_000; // Afhankelijk van volume precisie

//    // Interne opslag (deltas, scaled)
//    public uint OpenTimeOffset { get; set; } // Uint offset in minutes vanaf base time
//    public long OpenStorage { get; set; } // Absoluut voor eerste, anders delta scaled
//    public int HighDelta { get; set; } // Scaled delta from Open
//    public int LowDelta { get; set; } // Scaled delta from Open (positief)
//    public int CloseDelta { get; set; } // Scaled delta from Open
//    public double VolumeStorage { get; set; } // Scaled, uint want positief

//    // Constructor/logic om te encoden (in een manager class)
//    // Bijv. bij vullen: if (index == 0) OpenStorage = (long)(open * PriceScale); else OpenStorage = (long)((open - prev.Close) * PriceScale);

//    // Getters (vereist decoding met prev, dus beter in een array-manager)
//    // Voor simplicity: assume een ArrayCandleManager die absolutes cached of computed
//    public decimal Open => OpenStorage / (decimal)PriceScale;
//    public decimal High => Open + HighDelta / (decimal)PriceScale;
//    public decimal Low => Open - LowDelta / (decimal)PriceScale;
//    public decimal Close => Open + CloseDelta / (decimal)PriceScale;
//    public decimal Volume { readonly get => (decimal)VolumeStorage; set { VolumeStorage = (double)value; } }

//    public DateTime Date {get; set;} //=> BaseDate.AddMinutes(OpenTimeOffset); // BaseDate static
//    // ... andere properties
//}

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

[DebuggerDisplay("{ToDateTime(),nq} ({_minutes} min)")]
public readonly struct CandleTime : IEquatable<CandleTime>, IComparable<CandleTime>
{
    private const int SecondsPerMinute = 60;
    // Epoch is Monday 2010-01-04 so that epoch-relative modulo aligns weekly candles to Monday 00:00 UTC,
    // matching the Binance convention. Shifting by 3 days from Jan 1 (Friday) does not affect sub-daily
    // or daily intervals because 3 days is an exact multiple of their durations.
    public static readonly DateTime Epoch = new(2010, 1, 4, 0, 0, 0, DateTimeKind.Utc);
    private static readonly long EpochUnixSec = new DateTimeOffset(Epoch).ToUnixTimeSeconds();

    private readonly uint _minutes;

    public static readonly CandleTime MinValue = new(uint.MinValue);
    public static readonly CandleTime MaxValue = new(uint.MaxValue);

    public uint Minutes => _minutes;

    public CandleTime(uint minutes)
    {
        _minutes = minutes;
    }

    // ---------- Factory ----------

    public static CandleTime operator +(CandleTime time, uint minutes) => new(time._minutes + minutes);
    public static CandleTime operator -(CandleTime time, uint minutes) => new(time._minutes - minutes);

    public static bool operator ==(CandleTime time1, CandleTime time2)
    {
        return time1._minutes == time2._minutes;
    }

    public static bool operator !=(CandleTime time1, CandleTime time2)
    {
        return time1._minutes != time2._minutes;
    }


    public static bool operator <(CandleTime time1, CandleTime time2)
    {
        return time1._minutes < time2._minutes;
    }

    public static bool operator >(CandleTime time1, CandleTime time2)
    {
        return time1._minutes > time2._minutes;
    }

    public static bool operator <=(CandleTime time1, CandleTime time2)
    {
        return time1._minutes <= time2._minutes;
    }

    public static bool operator >=(CandleTime time1, CandleTime time2)
    {
        return time1._minutes >= time2._minutes;
    }

    public static bool operator <(CandleTime time, uint value) => time._minutes < value;
    public static bool operator >(CandleTime time, uint value) => time._minutes > value;
    public static bool operator <=(CandleTime time, uint value) => time._minutes <= value;
    public static bool operator >=(CandleTime time, uint value) => time._minutes >= value;
    public static bool operator ==(CandleTime time1, uint value) => time1._minutes == value;
    public static bool operator !=(CandleTime time1, uint value) => time1._minutes != value;
    public static uint operator %(CandleTime time, uint value) => time._minutes % value;
    public static CandleTime operator *(CandleTime time, uint value) => new(time._minutes * value);

    public static CandleTime operator +(CandleTime time, int value) => new((uint)(time._minutes + value));
    public static CandleTime operator +(CandleTime time, long value) => new((uint)(time._minutes + value));

    public static CandleTime operator -(CandleTime time, int value) => new((uint)(time._minutes - value));
    public static CandleTime operator -(CandleTime time, long value) => new((uint)(time._minutes - value));

    public static bool operator <(uint value, CandleTime time) => value < time._minutes;
    public static bool operator >(uint value, CandleTime time) => value > time._minutes;
    public static bool operator <=(uint value, CandleTime time) => value <= time._minutes;
    public static bool operator >=(uint value, CandleTime time) => value >= time._minutes;
    public static bool operator ==(uint value, CandleTime time) => value == time._minutes;
    public static bool operator !=(uint value, CandleTime time) => value != time._minutes;
    public static uint operator +(CandleTime end, CandleTime start) => end._minutes + start._minutes;
    public static uint operator -(CandleTime end, CandleTime start) => end._minutes - start._minutes;

    public CandleTime AddMinutes(int minutes) => new((uint)(_minutes + minutes));
    public CandleTime AddHours(int hours) => new((uint)(_minutes + (hours * 60)));
    public CandleTime AddDays(int days) => new((uint)(_minutes + (days * 1440)));

    public CandleTime AlignToIntervalMinutes(uint durationInMinutes)
    {
        uint remainder = _minutes % durationInMinutes;
        return new CandleTime(_minutes - remainder);
    }

    public static CandleTime FromUnixSeconds(long unixSec)
    {
        uint minutes = CandleTime.FromUnixSecondsInternal(unixSec);
        return new CandleTime(minutes);
    }

    public override string ToString() => $"{ToDateTime():yyyy-MM-dd HH:mm} ({_minutes:N0})";

    // ---------- Instance ----------

    public static CandleTime FromDateTime(DateTime date)
    {
        if (date.Kind != DateTimeKind.Utc)
            date = date.ToUniversalTime();

        ArgumentOutOfRangeException.ThrowIfLessThan(date, CandleTime.Epoch);
        long minutes = (long)(date - CandleTime.Epoch).TotalMinutes;
        return new CandleTime((uint)minutes);
    }

    public DateTime ToDateTime() => Epoch.AddMinutes(_minutes); //CandleTime.ToDateTimeInternal(_minutes);
    public DateTime ToLocalTime() => Epoch.AddMinutes(_minutes).ToLocalTime(); //CandleTime.ToDateTimeInternal(_minutes);
    public long ToUnixSeconds() => EpochUnixSec + ((long)_minutes * SecondsPerMinute); //CandleTime.ToUnixSecondsInternal(_minutes);

    // Align the DateTime parameter to minutes
    public static CandleTime AlignFromDateTime(DateTime datetime, uint intervalDuration)
    {
        // The intervalDuration is the amount of minutes
        DateTimeOffset dateTimeOffset = datetime.ToUniversalTime();
        long unix = dateTimeOffset.ToUnixTimeSeconds();
        if (intervalDuration != 0)
            unix -= unix % (intervalDuration * 60); // From minutes to seconds
        return new CandleTime(CandleTime.FromUnixSecondsInternal(unix));
    }

    public override bool Equals(object? obj) => obj is CandleTime other && Equals(other);
    public bool Equals(CandleTime other) => _minutes == other._minutes;
    public override int GetHashCode() => _minutes.GetHashCode();

    public int CompareTo(CandleTime other) => _minutes.CompareTo(other._minutes);


    // unix value -> uint minutes
    private static uint FromUnixSecondsInternal(long unixSec)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(unixSec, EpochUnixSec);
        long minutes = (unixSec - EpochUnixSec) / SecondsPerMinute;
        return (uint)minutes;
    }

    // uint minutes -> DateTime UTC
    //private static DateTime ToDateTimeInternal(uint minutes) => Epoch.AddMinutes(minutes);

    // uint minutes -> unix value
    //private static long ToUnixSecondsInternal(uint minutes) => EpochUnixSec + ((long)minutes * SecondsPerMinute);
}
