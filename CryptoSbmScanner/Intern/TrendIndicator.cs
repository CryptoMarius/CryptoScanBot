using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using System.Text;

namespace CryptoSbmScanner.Intern;

public class CryptoZigZagResult
{
    public string PointType { get; set; } // indicates a specific point and type e.g. H or L
    public double Value { get; set; }
    public CryptoCandle Candle { get; set; }
}

public class TrendIndicator
{
    //public List<CryptoZigZagResult> zigZagList = new List<CryptoZigZagResult>();
    private CryptoSymbol Symbol { get; set; }
    private CryptoInterval Interval { get; set; }
    private readonly SortedList<long, CryptoCandle> Candles;
    private bool ShowTrend { get; set; }
    public StringBuilder Log { get; set; }


    public TrendIndicator(CryptoSymbol symbol, CryptoInterval interval, bool showTrend = false)
    {
        Symbol = symbol;
        Interval = interval;
        ShowTrend = showTrend;
        Candles = Symbol.GetSymbolInterval(Interval.IntervalPeriod).CandleList;
    }


    /// <summary>
    /// ZigZag afkomstig uit de cAlgo wereld
    /// </summary>
    public CryptoTrendIndicator CalculateTrend()
    {
        // Methode 1 via een cAlgo ZigZag

        //GlobalData.AddTextToLogTab("");
        //GlobalData.AddTextToLogTab("");
        //GlobalData.AddTextToLogTab("ZigZagTest cAlgo#1");
        //GlobalData.AddTextToLogTab("");

        //List<CryptoCandle> history = CalculateHistory(this.Candles, 1000); // Toch ingekort
        List<CryptoCandle> history = Candles.Values.ToList();
        if (history.Count == 0)
        {
            // Hele discussies, maar als we niet genoeg candles hebben om een trend te berekenen
            // gebruiken we toch de sideway's om aan te geven dat het niet berekend kon worden.
            // Bij nieuwe munten, Flatliners (ethusdt) en andere gedrochten is het dus sideway's!
            //Signal.Reaction = string.Format("not enough quotes for {0} trend", interval.Name);
            return CryptoTrendIndicator.trendSideways;
        }

        TrendIndicatorZigZag1 zigZagTest = new();
        // Bied de candles een voor 1 aan
        for (int i = 0; i < history.Count; i++)
            zigZagTest.Calculate(history[i], true);

        // Maak van de gevonden punten een bruikbare ZigZag lijst
        List<CryptoZigZagResult> zigZagList = PickupZigZagValues(zigZagTest);

        CryptoTrendIndicator trend = InterpretationZigZagValues(zigZagList);
        return trend;
    }


    private List<CryptoZigZagResult> PickupZigZagValues(TrendIndicatorZigZag1 zigZagTest)
    {
        List<CryptoZigZagResult> zigZagList = new();

        if (Log != null)
        {
            Log.AppendLine("");
            Log.AppendLine("ZigZag points:");
        }

        //GlobalData.AddTextToLogTab("");
        //GlobalData.AddTextToLogTab("ZigZag points:");
        // De lows en highs in 1 lijst zetten voor interpretatie verderop
        // Deze indicator zet de candles net andersom (voila)
        for (int x = zigZagTest.Candles.Count - 1; x >= 0; x--)
        {
            CryptoCandle candle = zigZagTest.Candles[x];

            CryptoZigZagResult zigZagResult;
            if (zigZagTest._highBuffer[x] != 0)
            {
                if (Log != null)
                {
                    string s = string.Format("date={0} H {1:N8} rsi={2:N8}", candle.Date.ToLocalTime(), zigZagTest._highBuffer[x], candle.CandleData?.Rsi);
                    Log.AppendLine(s);
                }

                zigZagResult = new CryptoZigZagResult
                {
                    Candle = candle,
                    PointType = "H",
                    Value = (double)zigZagTest._highBuffer[x]
                };
                zigZagList.Add(zigZagResult);
            }

            if (zigZagTest._lowBuffer[x] != 0)
            {
                if (Log != null)
                {
                    string s = string.Format("date={0} L {1:N8} rsi={2:N8}", candle.Date.ToLocalTime(), zigZagTest._lowBuffer[x], candle.CandleData?.Rsi);
                    Log.AppendLine(s);
                }

                zigZagResult = new CryptoZigZagResult
                {
                    Candle = candle,
                    PointType = "L",
                    Value = (double)zigZagTest._lowBuffer[x]
                };
                zigZagList.Add(zigZagResult);
            }
        }
        return zigZagList;
    }

    /// <summary>
    /// Interpreteer de zigzag values (P&T) en identificeer de trend
    /// </summary>
    public CryptoTrendIndicator InterpretationZigZagValues(List<CryptoZigZagResult> zigZagList)
    {
        if (Log != null)
        {
            Log.AppendLine("");
            Log.AppendLine("ZigZag interpretation:");
        }

        CryptoTrendIndicator trend = CryptoTrendIndicator.trendSideways;

        // Zijn er meer dan 1 punt?, zoniet laat dan maar, want dan is er niets uit te extraheren
        if (zigZagList.Count < 2)
            return trend;


        // Pak de 1e 2 punten en bepaal een start situatie (de sideway's komt dan niet meer voor).
        int count = 0;
        double lastLow;
        double lastHigh;
        if (zigZagList[1].Value > zigZagList[0].Value)
        {
            lastLow = zigZagList[0].Value;
            lastHigh = zigZagList[1].Value;
            trend = CryptoTrendIndicator.trendBullish;
        }
        else
        {
            lastLow = zigZagList[1].Value;
            lastHigh = zigZagList[0].Value;
            trend = CryptoTrendIndicator.trendBearish;
        }


        // Nieuwe bepaling [NB: Er is discussie over de laatste zigzag waarde (market value ipv een low/high)]
        // Je wilt naar een soort van pairs toe (l,h) zodat je kan vergelijken met de vorige (l,h)
        // (?verwarring of je een (l,h) of (h,l) gebruikt, beide zou kunnen, misschien vanwege start situatie?
        // Maar maakt het uit, zolang je maar genoeg zigzag punten hebt boeit het niet?)
        CryptoZigZagResult zigZagResult;
        for (int i = 2; i < zigZagList.Count; i++)
        {
            zigZagResult = zigZagList[i];

            // Pickup last value
            double value;
            if (zigZagResult.PointType == "H")
                value = lastHigh;
            else
                value = lastLow;

            // Check Values
            switch (trend)
            {
                case CryptoTrendIndicator.trendBearish:
                    if (zigZagResult.Value > value)
                        count++;
                    else count = 0;
                    break;
                case CryptoTrendIndicator.trendBullish:
                    if (zigZagResult.Value <= value)
                        count++;
                    else count = 0;
                    break;

            }

            // Save the last value
            if (zigZagResult.PointType == "H")
                lastHigh = zigZagResult.Value;
            else
                lastLow = zigZagResult.Value;


            Log?.AppendLine(string.Format("date={0} {1} {2:N8} rsi={3:N8} count={4}, trend={5}", zigZagResult.Candle.Date.ToLocalTime(),
                    zigZagResult.PointType, zigZagResult.Value, zigZagResult.Candle.CandleData?.Rsi, count, trend));

            // switch trend if 2 values are opposite
            if (count > 1)
            {
                count = 0;
                if (trend == CryptoTrendIndicator.trendBearish)
                    trend = CryptoTrendIndicator.trendBullish;
                else if (trend == CryptoTrendIndicator.trendBullish)
                    trend = CryptoTrendIndicator.trendBearish;

                Log?.AppendLine("The trend switched");

            }
        }


        //s = string.Format("interval={0} count={1}", interval.Name, count);
        //GlobalData.AddTextToLogTab(s);
        //if (count < 3)
        //{
        //    GlobalData.AddTextToLogTab("reset to sideway's");
        //    trend = CryptoTrendIndicator.trendSideways; // trendBearish;
        //}

        // De zigzag heeft (denk ik) problemen met de laatste piek of dal, daarom het volgende ter correctie:
        // Als de allerlaatste candle alweer onder of boven de l/h staat is de trend niet meer geldig

        //CryptoCandle candleLast = history.Last();
        //if (trend == CryptoTrendIndicator.trendBullish)
        //{
        //    if (candleLast.Close < (decimal)low)
        //        trend = CryptoTrendIndicator.trendSideways; // trendBearish;
        //}
        //if (trend == CryptoTrendIndicator.trendBearish)
        //{
        //    if (candleLast.Close > (decimal)high)
        //        trend = CryptoTrendIndicator.trendSideways; // trendBullish;
        //}

        //GlobalData.AddTextToLogTab("");
        //GlobalData.AddTextToLogTab("Trend(2):");
        if (ShowTrend)
        {
            string s;
            if (trend == CryptoTrendIndicator.trendBullish)
                s = string.Format("{0} {1}, candles={2}, count={3}, trend=bullish", Symbol.Name, Interval.Name, Candles.Count, count);
            else if (trend == CryptoTrendIndicator.trendBearish)
                s = string.Format("{0} {1}, candles={2}, count={3}, trend=bearish", Symbol.Name, Interval.Name, Candles.Count, count);
            else
                s = string.Format("{0} {1}, candles={2}, count={3}, trend=sideway's?", Symbol.Name, Interval.Name, Candles.Count, count);
            GlobalData.AddTextToLogTab(s);

            Log?.AppendLine(s);
        }

        return trend;
    }

}
