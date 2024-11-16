using System.Text.Json.Serialization;

using CryptoScanBot.Core.Intern;
using Dapper.Contrib.Extensions;
using Skender.Stock.Indicators;

namespace CryptoScanBot.Core.Model;

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
    //[JsonIgnore]
    public DateTime DateLocal { get { return CandleTools.GetUnixDate(OpenTime).ToLocalTime(); } }

    [Computed]
    [JsonIgnore]
    public CandleIndicatorData? CandleData { get; set; }

    // Candles die on the fly zijn aangemaakt (niet confirmed) vanwege Kucoin
    [Computed]
    [JsonIgnore]
    public bool IsDuplicated { get; set; }
}

public class CryptoCandleList : SortedList<long, CryptoCandle>
{
}

//
// Voor een toekomstige Helperclass wellicht? (naar een StringStream oid)
//
//public void DumpSymbol()
//{
//    //Ter debug van een hardnekig probleem met het tonen van de signal
//    var csv = new StringBuilder();
//    var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", "OpenTime", "IntervalId", "Open", "High", "Low", "Close", "Volume");
//    csv.AppendLine(newLine);

//    Monitor.Enter(history);
//    try
//    {
//        for (int i = 0; i < history.Count; i++)
//        {
//            CryptoCandle candle = history[i];

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
//        Monitor.Exit(history);
//    }
//    string filename = System.IO.Path.GetDirectoryName((System.Reflection.Assembly.GetEntryAssembly().Location));
//    filename = filename + @"\data\" + Symbol.Exchange.Name + @"\Candles\" + Interval.Name + @"\";
//    System.IO.Directory.CreateDirectory(filename);
//    System.IO.File.WriteAllText(filename + Symbol.Name + "-" + Interval.Name + ".csv", csv.ToString());
//}
