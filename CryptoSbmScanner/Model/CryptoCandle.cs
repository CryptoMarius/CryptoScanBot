using CryptoSbmScanner.Intern;
using Skender.Stock.Indicators;

namespace CryptoSbmScanner.Model;

public class CryptoCandle : IQuote
{
    public long OpenTime { get; set; }
    public DateTime Date { get { return CandleTools.GetUnixDate(OpenTime); } }
    public DateTime DateLocal { get { return CandleTools.GetUnixDate(OpenTime).ToLocalTime(); } }

    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }

    public virtual CryptoSymbol Symbol { get; set; }
    public virtual CryptoInterval Interval { get; set; }
    public CandleIndicatorData CandleData { get; set; }

    public string ExtraText; // beetje quick en dirty
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
