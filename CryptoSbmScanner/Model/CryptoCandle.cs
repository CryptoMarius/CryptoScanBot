using System;
using Skender.Stock.Indicators;

namespace CryptoSbmScanner
{
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
    }
}
