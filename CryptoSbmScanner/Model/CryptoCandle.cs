using CryptoSbmScanner.Intern;
using Dapper.Contrib.Extensions;
using Skender.Stock.Indicators;

namespace CryptoSbmScanner.Model;

#if SQLDATABASE
[Table("Candle")]
#endif
public class CryptoCandle : IQuote
{
#if SQLDATABASE
    [Key]
    public int Id { get; set; }
    public int ExchangeId { get; set; }
    public int SymbolId { get; set; }
    public int IntervalId { get; set; }
#endif
    public long OpenTime { get; set; } // een long is 128 bit, het zou in een uint kunnen (delen door 60)
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }

    [Computed]
    public DateTime Date { get { return CandleTools.GetUnixDate(OpenTime); } }

    [Computed]
    public DateTime DateLocal { get { return CandleTools.GetUnixDate(OpenTime).ToLocalTime(); } }

    [Computed]
    public CandleIndicatorData CandleData { get; set; }

    //[Computed]
    //public string ExtraText { get; set; } // beetje quick en dirty voor een Excel export
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
