using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using Skender.Stock.Indicators;

namespace CryptoScanBot.Core.Signal.Experiment;

public class BbmaStrategyGrok2
{
    // Inputs
    public string MtfMode { get; set; } = "Both";
    public bool HtfCloseOnly { get; set; } = true;
    public int LookbackSig { get; set; } = 6;
    public bool StrictExtreme { get; set; } = true;

    // Dashboard Inputs
    public int DashWinHTF { get; set; } = 20;

    public enum BbmaEvent 
    {
        Extreme,
        ExtremeCombo,
        MHV,
        CSAK,
        Momentum,
        ReEntry,
    }

    public class AdminTimeFrame1
    {
        public List<Quote> candles1;

        // Results of lower timeframe Part1
        public bool UpTrend = false;
        public bool DownTrend = false;

        // CSD (Candle Stick Direction)
        public bool CsakBuyBaseCurrent = false;
        public bool CsakSellBaseCurrent = false; 

        public bool MomemtumBuyRawCurrent = false;
        public bool MomemtumSellRawCurrent = false;

        public bool ReEntryBuyRawCurrent = false;
        public bool ReEntrySellRawCurrent = false;

        public bool MhvBuyBaseCurrent = false;
        public bool MhvSellBaseCurrent = false;

        public bool[] ExtremeBuyBase = [];
        public bool[] ExtremeSellBase = [];

        public AdminTimeFrame1(List<Quote> candles)
        {
            candles1 = candles;
            int count = candles.Count;
            ExtremeBuyBase = new bool[count];
            ExtremeSellBase = new bool[count];
        }
    }


    // TODO:
    // The variabels can be renamed no in the class..
    // The prefixes are now resolved by the class


    public class AdminTimeFrame2
    {
        public List<Quote> candles2;

        public bool[] ExtremeBuyLtf = [];
        public bool[] ExtremeSellLtf = [];
        public bool[] htf2MhvBuyLtf = [];
        public bool[] htf2MhvSellLtf = [];
        // CSD
        public bool[] htf2CsakBuyLtf = [];
        public bool[] htf2CsakSellLtf = [];
        public bool[] htf2ReEntryBuyLtf = [];
        public bool[] htf2ReEntrySellLtf = [];
        public bool[] htf2UpLtf = [];
        public bool[] htf2DownLtf = [];

        public bool[] htf2ExtBuyBase = [];
        public bool[] htf2ExtSellBase = [];
        // CSD
        public bool[] htf2CsakBuyBase = [];
        public bool[] htf2CsakSellBase = [];
        public bool[] htf2ReEntryBuyBase = [];
        public bool[] htf2ReEntrySellBase = [];
        public bool[] htf2Up = [];
        public bool[] htf2Down = [];

        public bool[] htf2MhvBuyBase = [];
        public bool[] htf2MhvSellBase = [];

        public int[] htf2ExtBuyBSHist = [];
        public int[] htf2ExtSellBSHist = [];

        public AdminTimeFrame2(List<Quote> candles)
        {
            candles2 = candles;
            int count = candles.Count;
            htf2ExtBuyBase = new bool[count];
            htf2ExtSellBase = new bool[count];
            htf2CsakBuyBase = new bool[count];
            htf2CsakSellBase = new bool[count];
            htf2ReEntryBuyBase = new bool[count];
            htf2ReEntrySellBase = new bool[count];
            htf2Up = new bool[count];
            htf2Down = new bool[count];
        }
    }


    public class AdminTimeFrame3
    {
        public List<Quote> candles3;

        public bool DirBuyOK = false;
        public bool DirSellOK = false;

        public int htf2ExtremeBuyBS = 0;
        public int htf2ExtremeSellBS = 0;
        public int htf2MhvBuyBS = 0;
        public int htf2MhvSellBS = 0;

        public bool[] htf3UpLtf = [];
        public bool[] htf3DownLtf = [];

        public AdminTimeFrame3(List<Quote> candles)
        {
            this.candles3 = candles;
        }
    }


    public class SignalEventArgs : EventArgs
    {
        public CryptoTradeSide Side { get; }
        public BbmaEvent Event { get; }
        public string Message { get; }

        public SignalEventArgs(CryptoTradeSide side, BbmaEvent evnt, string message)
        {
            Side = side;
            Event = evnt;
            Message = message;
        }
    }


    public event EventHandler<SignalEventArgs>? SignalTriggered;


    static int GetSince(bool[] condition, int index, int maxLookback)
    {
        int since = 0;
        for (int j = index; j >= 0; j--)
        {
            if (condition[j])
                return since;
            since++;
            if (since > maxLookback)
                break;
        }
        return int.MaxValue;
    }



    // Helper methode om alert te triggeren
    private void TriggerAlert(CryptoTradeSide side, BbmaEvent evnt, string message)
    {
        // Log naar console (voor demo)
        //Console.WriteLine($"ALERT: {signalType} - {message} at {DateTime.Now}");

        //GlobalData.AddTextToLogTab($"{signalType} {message}");

        // Vuur event af (je kunt hier listeners aanhangen)
        SignalTriggered?.Invoke(this, new SignalEventArgs(side, evnt, message));
    }

    private List<Quote> ToQuotes(CryptoCandleList dict)
    {
        return dict?.OrderBy(x => x.Key).Select(x => new Quote
        {
            Date = DateTimeOffset.FromUnixTimeMilliseconds(x.Key).UtcDateTime,
            Open = x.Value.Open,
            High = x.Value.High,
            Low = x.Value.Low,
            Close = x.Value.Close
        }).ToList() ?? [];
    }

    private bool[] ProjectToLtf(List<Quote> htf, bool[] htfCondition, List<Quote> ltf)
    {
        bool[] ltfProj = new bool[ltf.Count];
        int k = 0;
        for (int i = 0; i < ltf.Count; i++)
        {
            while (k < htf.Count - 1 && htf[k + 1].Date <= ltf[i].Date)
                k++;
            ltfProj[i] = k < htf.Count && htfCondition[k];
        }
        return ltfProj;
    }

    private int FindHtfIndex(List<Quote> htf, DateTime ltfDate)
    {
        int low = 0;
        int high = htf.Count - 1;
        int result = -1;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (htf[mid].Date <= ltfDate)
            {
                result = mid;
                low = mid + 1;
            }
            else high = mid - 1;
        }
        return result;
    }

    private AdminTimeFrame1? tf1;
    private AdminTimeFrame2? tf2;
    private AdminTimeFrame3? tf3;

    void Part1(List<Quote> candles1, AdminTimeFrame1 tf1)
    {
        //*****************************************************************************************
        // LTF indicators
        //*****************************************************************************************
        var candles1Atr14 = candles1.GetAtr(14).ToList();
        var candles1Ema50 = candles1.GetEma(50).ToList();
        var candles1Bb = candles1.GetBollingerBands(20, 2.0).ToList();

        var candles1xH = candles1.Use(CandlePart.High);
        var candles1Wma5H = candles1xH.GetWma(05).ToList();
        var candles1Wma10H = candles1xH.GetWma(10).ToList();

        var candles1xL = candles1.Use(CandlePart.Low);
        var candles1Wma5L = candles1xL.GetWma(05).ToList();
        var candles1Wma10L = candles1xL.GetWma(10).ToList();

        int candles1Count = candles1.Count;

        // LTF conditions historical
        bool[] momemtumBuyRaw = new bool[candles1Count];
        bool[] momemtumSellRaw = new bool[candles1Count];
        bool[] reBuyRaw = new bool[candles1Count];
        bool[] reSellRaw = new bool[candles1Count];

        for (int j = 0; j < candles1Count; j++)
        {
            var candle = candles1[j];
            decimal jMa5H = (decimal)(candles1Wma5H[j].Wma ?? 0);
            decimal jMa10H = (decimal)(candles1Wma10H[j].Wma ?? 0);
            decimal jMa5L = (decimal)(candles1Wma5L[j].Wma ?? 0);
            decimal jMa10L = (decimal)(candles1Wma10L[j].Wma ?? 0);
            decimal jUpper = (decimal)(candles1Bb[j].UpperBand ?? 0);
            decimal jMiddle = (decimal)(candles1Bb[j].Sma ?? 0);
            decimal jLower = (decimal)(candles1Bb[j].LowerBand ?? 0);
            decimal jAtr = (decimal)(candles1Atr14[j].Atr ?? 0);

            // rejection on lower or upper band or wms5 or wms10 breaking the bb
            // This test on all the possible extremes is not complete (but does it matter that much)
            tf1.ExtremeBuyBase[j] = (candle.Low < jLower && candle.Close > jLower) && (StrictExtreme ? jMa5L < jLower : jMa5L < jLower || jMa10L < jLower);
            tf1.ExtremeSellBase[j] = (candle.High > jUpper && candle.Close < jUpper) && (StrictExtreme ? jMa5H > jUpper : jMa5H > jUpper || jMa10H > jUpper);

            bool jBigBody = Math.Abs(candle.Close - candle.Open) > (0.6m * jAtr);
            momemtumBuyRaw[j] = (candle.Close > jUpper) && jBigBody;
            momemtumSellRaw[j] = (candle.Close < jLower) && jBigBody;

            bool jUpTrend = candle.Close > (decimal)(candles1Ema50[j].Ema ?? 0);
            bool jDownTrend = candle.Close < (decimal)(candles1Ema50[j].Ema ?? 0);

            reBuyRaw[j] = jUpTrend && (candle.Low <= Math.Max(jMa5L, jMa10L)) && candle.Close >= jMiddle;
            reSellRaw[j] = jDownTrend && (candle.High >= Math.Min(jMa5H, jMa10H)) && candle.Close <= jMiddle;
        }

        int candles1LastIndex = candles1.Count - 1;
        var candleLast = candles1[candles1LastIndex];
        decimal lEma50 = (decimal)(candles1Ema50[candles1LastIndex].Ema ?? 0);
        tf1.UpTrend = candleLast.Close > lEma50;
        tf1.DownTrend = candleLast.Close < lEma50;

        double lower = candles1Bb[candles1LastIndex].LowerBand ?? 0;
        int sinceExtBuy = GetSince(tf1.ExtremeBuyBase, candles1LastIndex, LookbackSig);
        tf1.MhvBuyBaseCurrent = (candleLast.Low < (decimal)lower && candleLast.Close > (decimal)lower) && sinceExtBuy < LookbackSig;

        double upper = candles1Bb[candles1LastIndex].UpperBand ?? 0;
        int sinceExtSell = GetSince(tf1.ExtremeSellBase, candles1LastIndex, LookbackSig);
        tf1.MhvSellBaseCurrent = (candleLast.High > (decimal)upper && candleLast.Close < (decimal)upper) && sinceExtSell < LookbackSig;

        // Historical MHV for barsSinceMhv
        int[] sinceExtBuyHist = Enumerable.Range(0, candles1Count).Select(j => GetSince(tf1.ExtremeBuyBase, j, LookbackSig)).ToArray();
        int[] sinceExtSellHist = Enumerable.Range(0, candles1Count).Select(j => GetSince(tf1.ExtremeSellBase, j, LookbackSig)).ToArray();

        bool[] mhvBuyBaseHist = new bool[candles1Count];
        bool[] mhvSellBaseHist = new bool[candles1Count];
        for (int j = 0; j < candles1Count; j++)
        {
            var candle = candles1[j];
            double jLower = candles1Bb[j].LowerBand ?? 0;
            double jUpper = candles1Bb[j].UpperBand ?? 0;

            mhvBuyBaseHist[j] = (candle.Low < (decimal)jLower && candle.Close > (decimal)jLower) && sinceExtBuyHist[j] < LookbackSig;
            mhvSellBaseHist[j] = (candle.High > (decimal)jUpper && candle.Close < (decimal)jUpper) && sinceExtSellHist[j] < LookbackSig;
        }

        int barsSinceMhvBuy = GetSince(mhvBuyBaseHist, candles1LastIndex, LookbackSig);
        int barsSinceMhvSell = GetSince(mhvSellBaseHist, candles1LastIndex, LookbackSig);

        decimal lAtr = (decimal)(candles1Atr14[candles1LastIndex].Atr ?? 0);
        bool bigBody = Math.Abs(candleLast.Close - candleLast.Open) > (0.6m * lAtr);
        double middle = candles1Bb[candles1LastIndex].Sma ?? 0;
        tf1.CsakBuyBaseCurrent = bigBody && candleLast.Close > (decimal)middle && (sinceExtBuy < LookbackSig || barsSinceMhvBuy < LookbackSig);
        tf1.CsakSellBaseCurrent = bigBody && candleLast.Close < (decimal)middle && (sinceExtSell < LookbackSig || barsSinceMhvSell < LookbackSig);

        tf1.MomemtumBuyRawCurrent = momemtumBuyRaw[candles1LastIndex];
        tf1.MomemtumSellRawCurrent = momemtumSellRaw[candles1LastIndex];

        tf1.ReEntryBuyRawCurrent = reBuyRaw[candles1LastIndex];
        tf1.ReEntrySellRawCurrent = reSellRaw[candles1LastIndex];
    }


    void Part2(List<Quote> candles2, AdminTimeFrame2 tf2, AdminTimeFrame1 tf1)
    {
        var htf2Atr = candles2.GetAtr(14).ToList();
        var htf2Ema50 = candles2.GetEma(50).ToList();
        var htf2Bb = candles2.GetBollingerBands(20, 2.0).ToList();

        var htf2High = candles2.Use(CandlePart.High);
        var htf2Ma5H = htf2High.GetWma(05).ToList();
        var htf2Ma10H = htf2High.GetWma(10).ToList();

        var htf2Low = candles2.Use(CandlePart.Low);
        var htf2Ma5L = htf2Low.GetWma(05).ToList();
        var htf2Ma10L = htf2Low.GetWma(10).ToList();


        int m = candles2.Count;
        for (int j = 0; j < m; j++)
        {
            var candle = candles2[j];
            double jMiddle = htf2Bb[j].Sma ?? 0;
            double jUpper = htf2Bb[j].UpperBand ?? 0;
            double jLower = htf2Bb[j].LowerBand ?? 0;
            double jMa5H = htf2Ma5H[j].Wma ?? 0;
            double jMa10H = htf2Ma10H[j].Wma ?? 0;
            double jMa5L = htf2Ma5L[j].Wma ?? 0;
            double jMa10L = htf2Ma10L[j].Wma ?? 0;

            double jAtr = htf2Atr[j].Atr ?? 0;
            bool jBigBody = Math.Abs(candle.Close - candle.Open) > (decimal)(0.6 * jAtr);

            tf2.htf2ExtBuyBase[j] = (candle.Low < (decimal)jLower && candle.Close > (decimal)jLower) && (StrictExtreme ? jMa5L < jLower : jMa5L < jLower || jMa10L < jLower);
            tf2.htf2ExtSellBase[j] = (candle.High > (decimal)jUpper && candle.Close < (decimal)jUpper) && (StrictExtreme ? jMa5H > jUpper : jMa5H > jUpper || jMa10H > jUpper);

            tf2.htf2Up[j] = candle.Close > (decimal)(htf2Ema50[j].Ema ?? 0);
            tf2.htf2Down[j] = candle.Close < (decimal)(htf2Ema50[j].Ema ?? 0);

            tf2.htf2CsakBuyBase[j] = jBigBody && candle.Close > (decimal)jMiddle;
            tf2.htf2CsakSellBase[j] = jBigBody && candle.Close < (decimal)jMiddle;

            tf2.htf2ReEntryBuyBase[j] = tf2.htf2Up[j] && (candle.Low <= (decimal)Math.Max(jMa5L, jMa10L)) && candle.Close >= (decimal)jMiddle;
            tf2.htf2ReEntrySellBase[j] = tf2.htf2Down[j] && (candle.High >= (decimal)Math.Min(jMa5H, jMa10H)) && candle.Close <= (decimal)jMiddle;
        }

        tf2.htf2ExtBuyBSHist = Enumerable.Range(0, m).Select(j => GetSince(tf2.htf2ExtBuyBase, j, LookbackSig)).ToArray();
        tf2.htf2ExtSellBSHist = Enumerable.Range(0, m).Select(j => GetSince(tf2.htf2ExtSellBase, j, LookbackSig)).ToArray();

        tf2.htf2MhvBuyBase = new bool[m];
        tf2.htf2MhvSellBase = new bool[m];
        for (int j = 0; j < m; j++)
        {
            var candle = candles2[j];
            double jLower = htf2Bb[j].LowerBand ?? 0;
            double jUpper = htf2Bb[j].UpperBand ?? 0;

            tf2.htf2MhvBuyBase[j] = (candle.Low < (decimal)jLower && candle.Close > (decimal)jLower) && tf2.htf2ExtBuyBSHist[j] < LookbackSig;
            tf2.htf2MhvSellBase[j] = (candle.High > (decimal)jUpper && candle.Close < (decimal)jUpper) && tf2.htf2ExtSellBSHist[j] < LookbackSig;
        }

        // Project HTF to LTF
        tf2.ExtremeBuyLtf = ProjectToLtf(candles2, tf2.htf2ExtBuyBase, tf1.candles1);
        tf2.ExtremeSellLtf = ProjectToLtf(candles2, tf2.htf2ExtSellBase, tf1.candles1);
        tf2.htf2MhvBuyLtf = ProjectToLtf(candles2, tf2.htf2MhvBuyBase, tf1.candles1);
        tf2.htf2MhvSellLtf = ProjectToLtf(candles2, tf2.htf2MhvSellBase, tf1.candles1);
        tf2.htf2CsakBuyLtf = ProjectToLtf(candles2, tf2.htf2CsakBuyBase, tf1.candles1);
        tf2.htf2CsakSellLtf = ProjectToLtf(candles2, tf2.htf2CsakSellBase, tf1.candles1);
        tf2.htf2ReEntryBuyLtf = ProjectToLtf(candles2, tf2.htf2ReEntryBuyBase, tf1.candles1);
        tf2.htf2ReEntrySellLtf = ProjectToLtf(candles2, tf2.htf2ReEntrySellBase, tf1.candles1);
        tf2.htf2UpLtf = ProjectToLtf(candles2, tf2.htf2Up, tf1.candles1);
        tf2.htf2DownLtf = ProjectToLtf(candles2, tf2.htf2Down, tf1.candles1);
    }



    void Part3(List<Quote> candles3, AdminTimeFrame3 tf3, AdminTimeFrame2 tf2, AdminTimeFrame1 tf1)
    {
        bool[] htf3Up = new bool[candles3.Count];
        bool[] htf3Down = new bool[candles3.Count];
        var htf3Ema50 = candles3.GetEma(50).ToList();
        for (int j = 0; j < candles3.Count; j++)
        {
            htf3Up[j] = candles3[j].Close > (decimal)(htf3Ema50[j].Ema ?? 0);
            htf3Down[j] = candles3[j].Close < (decimal)(htf3Ema50[j].Ema ?? 0);
        }

        tf3.htf3UpLtf = ProjectToLtf(candles3, htf3Up, tf1.candles1);
        tf3.htf3DownLtf = ProjectToLtf(candles3, htf3Down, tf1.candles1);


        // Anti-repaint: shift to previous HTF bar if HtfCloseOnly
        // Wait for the higher timeframes(?) to complete its candle
        if (HtfCloseOnly)
        {
            for (int j = 0; j < tf1.candles1.Count; j++)
            {
                int k = FindHtfIndex(tf2.candles2, tf1.candles1[j].Date);
                if (k >= 1)
                    k--;

                if (k >= 0)
                {
                    tf2.ExtremeBuyLtf[j] = tf2.htf2ExtBuyBase[k];
                    tf2.ExtremeSellLtf[j] = tf2.htf2ExtSellBase[k];
                    tf2.htf2MhvBuyLtf[j] = tf2.htf2MhvBuyBase[k];
                    tf2.htf2MhvSellLtf[j] = tf2.htf2MhvSellBase[k];
                    tf2.htf2CsakBuyLtf[j] = tf2.htf2CsakBuyBase[k];
                    tf2.htf2CsakSellLtf[j] = tf2.htf2CsakSellBase[k];
                    tf2.htf2ReEntryBuyLtf[j] = tf2.htf2ReEntryBuyBase[k];
                    tf2.htf2ReEntrySellLtf[j] = tf2.htf2ReEntrySellBase[k];
                    tf2.htf2UpLtf[j] = tf2.htf2Up[k];
                    tf2.htf2DownLtf[j] = tf2.htf2Down[k];
                }
                else
                {
                    tf2.ExtremeBuyLtf[j] = false;
                    tf2.ExtremeSellLtf[j] = false;
                    tf2.htf2MhvBuyLtf[j] = false;
                    tf2.htf2MhvSellLtf[j] = false;
                    tf2.htf2CsakBuyLtf[j] = false;
                    tf2.htf2CsakSellLtf[j] = false;
                    tf2.htf2ReEntryBuyLtf[j] = false;
                    tf2.htf2ReEntrySellLtf[j] = false;
                    tf2.htf2UpLtf[j] = false;
                    tf2.htf2DownLtf[j] = false;
                }
            }

            if (true)
            {
                for (int j = 0; j < tf1.candles1.Count; j++)
                {
                    int k = FindHtfIndex(candles3, tf1.candles1[j].Date);
                    if (k >= 1)
                        k--;
                    if (k >= 0)
                    {
                        tf3.htf3UpLtf[j] = htf3Up[k];
                        tf3.htf3DownLtf[j] = htf3Down[k];
                    }
                    else
                    {
                        tf3.htf3UpLtf[j] = false;
                        tf3.htf3DownLtf[j] = false;
                    }
                }
            }
        }

        // HTF BS on LTF scale
        int candles1LastIndex = tf1.candles1.Count - 1;
        tf3.htf2ExtremeBuyBS = GetSince(tf2.ExtremeBuyLtf, candles1LastIndex, DashWinHTF);
        tf3.htf2ExtremeSellBS = GetSince(tf2.ExtremeSellLtf, candles1LastIndex, DashWinHTF);
        tf3.htf2MhvBuyBS = GetSince(tf2.htf2MhvBuyLtf, candles1LastIndex, DashWinHTF);
        tf3.htf2MhvSellBS = GetSince(tf2.htf2MhvSellLtf, candles1LastIndex, DashWinHTF);
        //int htf2ReBuyBS = GetSince(tf2.htf2ReEntryBuyLtf, candles1LastIndex, DashWinHTF);
        //int htf2ReSellBS = GetSince(tf2.htf2ReEntrySellLtf, candles1LastIndex, DashWinHTF);

        // Direction
        bool dirOn = MtfMode != "Off";
        tf3.DirBuyOK = !dirOn || (MtfMode == "TF1 only" ? tf1.UpTrend && tf2.htf2UpLtf[candles1LastIndex] :
                                   MtfMode == "Any" ? tf1.UpTrend && (tf2.htf2UpLtf[candles1LastIndex] || (true && tf3.htf3UpLtf[candles1LastIndex])) :
                                   tf1.UpTrend && tf2.htf2UpLtf[candles1LastIndex] && (!true || tf3.htf3UpLtf[candles1LastIndex]));

        tf3.DirSellOK = !dirOn || (MtfMode == "TF1 only" ? tf1.DownTrend && tf2.htf2DownLtf[candles1LastIndex] :
                                    MtfMode == "Any" ? tf1.DownTrend && (tf2.htf2DownLtf[candles1LastIndex] || (true && tf3.htf3DownLtf[candles1LastIndex])) :
                                    tf1.DownTrend && tf2.htf2DownLtf[candles1LastIndex] && (!true || tf3.htf3DownLtf[candles1LastIndex]));

    }


    // Results
    // Extreme
    public bool ExtremeBuy { get; private set; }
    public bool ExtremeSell { get; private set; }
    // Loss of Momentum
    public bool MhvBuy { get; private set; }
    public bool MhvSell { get; private set; }
    // Break of MA20
    public bool CsakBuy { get; private set; }
    public bool CsakSell { get; private set; }
    // CSM Candle Stick Momentum
    public bool MomemtumBuy { get; private set; }
    public bool MomemtumSell { get; private set; }
    // Reentry
    public bool ReEntryBuy { get; private set; }
    public bool ReEntrySell { get; private set; }
    //? ehh? Extreme Combo? 
    public bool ExtComboBuy { get; private set; }
    public bool ExtComboSell { get; private set; }
    
    public void Compute(CryptoCandleList candleList1, CryptoCandleList candleList2, CryptoCandleList? candleList3)
    {
        if (candleList1.Count == 0)
            return;

        var candles1 = ToQuotes(candleList1);

        // Results of lower timeframe Part1
        tf1 = new AdminTimeFrame1(candles1);
        Part1(candles1, tf1);

        // htf2
        var candles2 = ToQuotes(candleList2);
        tf2 = new AdminTimeFrame2(candles2);
        Part2(candles2, tf2, tf1);

        // htf3 direction
        var candles3 = ToQuotes(candleList3!);
        tf3 = new(candles3);
        Part3(candles3, tf3, tf2, tf1);


        // Trigger alerts
        int candles1LastIndex = tf1.candles1.Count - 1;
        ExtremeBuy = tf1.ExtremeBuyBase[candles1LastIndex];
        ExtremeSell = tf1.ExtremeSellBase[candles1LastIndex];
        if (ExtremeBuy) TriggerAlert(CryptoTradeSide.Long, BbmaEvent.Extreme, "Extreme BUY detected");
        if (ExtremeSell) TriggerAlert(CryptoTradeSide.Short, BbmaEvent.Extreme, "Extreme SELL detected");

        MhvBuy = tf1.MhvBuyBaseCurrent;
        MhvSell = tf1.MhvSellBaseCurrent;
        if (MhvBuy) TriggerAlert(CryptoTradeSide.Long, BbmaEvent.MHV, "MHV BUY detected");
        if (MhvSell) TriggerAlert(CryptoTradeSide.Short, BbmaEvent.MHV, "MHV SELL detected");

        CsakBuy = tf1.CsakBuyBaseCurrent;
        CsakSell = tf1.CsakSellBaseCurrent;
        if (CsakBuy) TriggerAlert(CryptoTradeSide.Long, BbmaEvent.CSAK, "CSAK BUY detected");
        if (CsakSell) TriggerAlert(CryptoTradeSide.Short, BbmaEvent.CSAK, "CSAK SELL detected");

        MomemtumBuy = tf1.MomemtumBuyRawCurrent;
        MomemtumSell = tf1.MomemtumSellRawCurrent;
        if (MomemtumBuy) TriggerAlert(CryptoTradeSide.Long, BbmaEvent.Momentum, "Momentum BUY detected");
        if (MomemtumSell) TriggerAlert(CryptoTradeSide.Short, BbmaEvent.Momentum, "Momentum SELL detected");

        ReEntryBuy = tf1.ReEntryBuyRawCurrent && tf3.DirBuyOK;
        ReEntrySell = tf1.ReEntrySellRawCurrent && tf3.DirSellOK;
        if (ReEntryBuy) TriggerAlert(CryptoTradeSide.Long, BbmaEvent.ReEntry, "Re-entry BUY detected");
        if (ReEntrySell) TriggerAlert(CryptoTradeSide.Short, BbmaEvent.ReEntry, "Re-entry SELL detected");

        ExtComboBuy = ReEntryBuy && (tf3.htf2ExtremeBuyBS < LookbackSig || tf3.htf2MhvBuyBS < LookbackSig);
        ExtComboSell = ReEntrySell && (tf3.htf2ExtremeSellBS < LookbackSig || tf3.htf2MhvSellBS < LookbackSig);
        if (ExtComboBuy) TriggerAlert(CryptoTradeSide.Long, BbmaEvent.ExtremeCombo, "Extreme Combo BUY detected");
        if (ExtComboSell) TriggerAlert(CryptoTradeSide.Short, BbmaEvent.ExtremeCombo, "Extreme Combo SELL detected");


        //// TP/SL Inputs
        //public string Tp2Mode { get; set; } = "EMA50";
        //// TP/SL
        //double tp1Buy = middle;
        //double tp2Buy = Tp2Mode == "EMA50" ? lEma50 : upper;
        //double slBuy = Math.Min(lMa10L, lower) - 0.5 * lAtr;

        //double tp1Sell = middle;
        //double tp2Sell = Tp2Mode == "EMA50" ? lEma50 : lower;
        //double slSell = Math.Max(lMa10H, upper) + 0.5 * lAtr;

        // Dashboard example
        //if (ShowDash)
        //{
        //    Console.WriteLine("Trend: " + (UpTrend ? "UP" : DownTrend ? "DOWN" : "FLAT"));
        //}
    }
}