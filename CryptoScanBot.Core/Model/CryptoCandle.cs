using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Signal;

using Dapper.Contrib.Extensions;

using Skender.Stock.Indicators;

using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Model;


// new idea:
// We specify candle.open etc in uint(4) / ticksize instead of decimal(8).
// We could spefify the rest in an offset of the open
// this saves 4 * 8 - 4 * 4 = 16 bytes per record (might even use smallint for offset?)
//
// Additional:
// Specify the opentime in uint(4) instead of long(8)
// save ~20 bytes per candle * 4.000.000 candles = 80 Mb (+more because of dictionary key's)
// but its a lot of work..

[Serializable]
public class CryptoCandle : IQuote
{
    public long OpenTime { get; set; } // een long is 128 bit, het zou in een uint kunnen (delen door 60)
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
#if SUPPORTBASEVOLUME
    public decimal BaseVolume { get; set; }
#endif

    [Computed]
    public DateTime Date { get { return CandleTools.GetUnixDate(OpenTime); } }

    [Computed]
    [JsonIgnore]
    public DateTime DateLocal { get { return CandleTools.GetUnixDate(OpenTime).ToLocalTime(); } }

    [Computed]
    [JsonIgnore]
    public CandleIndicatorData? CandleData { get; set; }

    // Candles die on the fly zijn aangemaakt (niet confirmed) vanwege Kucoin
    [Computed]
    [JsonIgnore]
    public bool IsDuplicated { get; set; }
}

public class CryptoCandleList : SortedList<long, CryptoCandle> // experiment via SortedDictionary? SortedList TrimExcess!!
{
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
//            candle.Date.ToString(),
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
