////### 
////### Dynamic SR Lines
////###
////### User		Date 		
////### ------	-------- 	
////### NT_JoshG	Jan 2018 	
////###
////
////
//// https://github.com/Jake0303/CryptoTradingBot/blob/master/DynamicSRLines.cs
////
////
////using System;
////using System.Collections.Generic;
////using System.ComponentModel;
////using System.ComponentModel.DataAnnotations;
////using System.Linq;
////using System.Line;
////using System.Threading.Tasks;
////using System.Windows;
////using System.Windows.Input;
////using System.Windows.Media;
////using System.Xml.Serialization;
////using NinjaTrader.Cbi;
////using NinjaTrader.Gui;
////using NinjaTrader.Gui.Chart;
////using NinjaTrader.Gui.SuperDom;
////using NinjaTrader.Gui.Tools;
////using NinjaTrader.Data;
////using NinjaTrader.NinjaScript;
////using NinjaTrader.Core.FloatingPoint;
////using NinjaTrader.NinjaScript.DrawingTools;
////using CryptoScanner.Core.Core;
////using CryptoScanner.Core.Model;

//using OxyPlot;
//using OxyPlot.Series;

//using Skender.Stock.Indicators;


//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Line;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Input;
//using System.Xml.Serialization;

////This namespace holds Indicators in this folder and is required. Do not change it. 
//namespace CryptoScanner.ZoneVisualisation.Chart;
//{

//    public class DynamicSRLines // : Indicator
//{
//    int PivotStrength = 5;
//    int MaxLookBackBars = 200;
//    int MaxLevels = 3;
//    int CurrentBar;


//    public int i = 0, j = 0, x = 0;
//    int LastBar = 0;
//    int pxAbove = 0, countAbove = 0, maxCountAbove = 0;
//    int pxBelow = 0, countBelow = 0, maxCountBelow = 0;
//    double p = 0, p1 = 0, level = 0;
//    string str = "";

//    public struct PRICE_SWING
//    {
//        public int Type;
//        public int Bar;
//        public double Price;
//    }
//    private double l1SupportPrice;
//    private double l1ResistancePrice;
//    PRICE_SWING listEntry;
//    List<PRICE_SWING> pivot = new List<PRICE_SWING>();
//    //NinjaTrader.Gui.Tools.SimpleFont boldFont = new NinjaTrader.Gui.Tools.SimpleFont("Arial", 10) { Size = 25, Bold = true };
//    //NinjaTrader.Gui.Tools.SimpleFont textFont = new NinjaTrader.Gui.Tools.SimpleFont("Times", 8) { Size = 25, Bold = false };
//    //protected override void OnStateChange()
//    //{

//    //    if (State == State.SetDefaults)
//    //    {
//    //        Description = @"Draws nearest level of S/R lines above and below current market based on historical price swing High/Low pivots";
//    //        Name = "DynamicSRLines";
//    //        Calculate = Calculate.OnBarClose;
//    //        IsOverlay = true;
//    //        DisplayInDataBox = true;
//    //        DrawOnPricePanel = true;
//    //        DrawHorizontalGridLines = true;
//    //        DrawVerticalGridLines = true;
//    //        PaintPriceMarkers = true;
//    //        ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
//    //        IsSuspendedWhileInactive = true;
//    //        PivotStrength = 5;
//    //        MaxLookBackBars = 200;
//    //        PivotTickDiff = 10;
//    //        ZoneTickSize = 2;
//    //        MaxLevels = 3;
//    //        ShowPivots = false;
//    //        ColorBelow = Brushes.Blue;
//    //        ColorAbove = Brushes.Red;

//    //    }
//    //    else if (State == State.Configure)
//    //    {
//    //    }
//    //}


//    protected void OnBarUpdate()
//    {
//        if (CurrentBar <= MaxLookBackBars)
//            return;

//        if (LastBar != CurrentBar)
//        {
//            x = HighestBar(High, (PivotStrength * 2) + 1);
//            if (x == PivotStrength)
//            {
//                listEntry.Type = +1;
//                listEntry.Price = High[x];
//                listEntry.Bar = CurrentBar - x;
//                pivot.Add(listEntry);

//                if (pivot.Count > MaxLookBackBars)
//                {
//                    pivot.RemoveAt(0);
//                }
//            }

//            x = LowestBar(Low, (PivotStrength * 2) + 1);
//            if (x == PivotStrength)
//            {
//                listEntry.Type = -1;
//                listEntry.Price = Low[x];
//                listEntry.Bar = CurrentBar - x;
//                pivot.Add(listEntry);

//                if (pivot.Count > MaxLookBackBars)
//                {
//                    pivot.RemoveAt(0);
//                }
//            }

//            if (CurrentBar >= Bars.Count - 2 && CurrentBar > MaxLookBackBars)
//            {
//                p = Close[0]; p1 = 0; str = "";
//                for (level = 1; level <= MaxLevels; level++)
//                {
//                    p = get_sr_level(p, +1);
//                    Draw.Rectangle(this, "resZone" + level, false, 0, p - ((ZoneTickSize * TickSize) / 2), MaxLookBackBars, p + ((ZoneTickSize * TickSize) / 2), ColorAbove, ColorAbove, 30);
//                    Draw.Line(this, "resLine" + level, false, 0, p - ((ZoneTickSize * TickSize) / 2), MaxLookBackBars, p - ((ZoneTickSize * TickSize) / 2), ColorAbove, DashStyleHelper.Solid, 1);
//                    if (level == 1)
//                        l1ResistancePrice = (p - ((ZoneTickSize * TickSize) / 2));

//                    str = (p == p1) ? str + " L" + level : "L" + level;
//                    Draw.Line(this, "resTag" + level, false, "\t    " + str, -5, p, 0, ColorAbove, boldFont, TextAlignment.Right, Brushes.Transparent, Brushes.Transparent, 0);
//                    p1 = p; p += (PivotTickDiff * TickSize);
//                }

//                p = Close[0]; p1 = 0; str = "";
//                for (level = 1; level <= MaxLevels; level++)
//                {
//                    p = get_sr_level(p, -1);
//                    Draw.Rectangle(this, "supZone" + level, false, 0, p - ((ZoneTickSize * TickSize) / 2), MaxLookBackBars, p + ((ZoneTickSize * TickSize) / 2), ColorBelow, ColorBelow, 30);
//                    Draw.Line(this, "supLine" + level, false, 0, p - ((ZoneTickSize * TickSize) / 2), MaxLookBackBars, p - ((ZoneTickSize * TickSize) / 2), ColorBelow, DashStyleHelper.Solid, 1);
//                    str = (p == p1) ? str + " L" + level : "L" + level;
//                    if (level == 1)
//                        l1SupportPrice = (p - ((ZoneTickSize * TickSize) / 2));
//                    Draw.Line(this, "supTag" + level, false, "\t    " + str, -5, p, 0, ColorBelow, boldFont, TextAlignment.Right, Brushes.Transparent, Brushes.Transparent, 0);
//                    p1 = p; p -= (PivotTickDiff * TickSize);
//                }
//            }
//        }
//        LastBar = CurrentBar;
//    }


//    double get_sr_level(double refPrice, int pos)
//    {
//        int i = 0, j = 0;
//        double levelPrice = 0;

//        if (CurrentBar >= Bars.Count - 2 && CurrentBar > MaxLookBackBars)
//        {
//            maxCountAbove = 0; maxCountBelow = 0; pxAbove = -1; pxBelow = -1;
//            for (i = pivot.Count - 1; i >= 0; i--)
//            {
//                countAbove = 0; countBelow = 0;
//                if (pivot[i].Bar < CurrentBar - MaxLookBackBars) break;
//                for (j = 0; j < pivot.Count - 1; j++)
//                {
//                    if (pivot[j].Bar > CurrentBar - MaxLookBackBars)
//                    {
//                        if (pos > 0 && pivot[i].Price >= refPrice)
//                        {
//                            if (Math.Abs(pivot[i].Price - pivot[j].Price) / TickSize <= PivotTickDiff)
//                            {
//                                countAbove++;
//                            }
//                            if (countAbove > maxCountAbove)
//                            {
//                                maxCountAbove = countAbove;
//                                levelPrice = pivot[i].Price;
//                                pxAbove = i;
//                            }
//                        }
//                        else
//                            if (pos < 0 && pivot[i].Price <= refPrice)
//                        {
//                            if (Math.Abs(pivot[i].Price - pivot[j].Price) / TickSize <= PivotTickDiff)
//                            {
//                                countBelow++;
//                            }
//                            if (countBelow > maxCountBelow)
//                            {
//                                maxCountBelow = countBelow;
//                                levelPrice = pivot[i].Price;
//                                pxBelow = i;
//                            }
//                        }
//                    }
//                }
//            }

//            if (pos > 0)
//            {
//                levelPrice = (pxAbove >= 0) ? pivot[pxAbove].Price : High[HighestBar(High, MaxLookBackBars)];
//            }
//            if (pos < 0)
//            {
//                levelPrice = (pxBelow >= 0) ? pivot[pxBelow].Price : Low[LowestBar(Low, MaxLookBackBars)];
//            }

//        }
//        return Instrument.MasterInstrument.RoundToTickSize(levelPrice);

//    }





//    //#region Properties
//    //[NinjaScriptProperty]
//    //[Range(1, int.MaxValue)]
//    //[Display(Name = "Swing Pivot Strength", Description = "How many bars should be on each side of Swing Hi/Low", Order = 1, GroupName = "Parameters")]
//    //public int PivotStrength
//    //{ get; set; }

//    //[NinjaScriptProperty]
//    //[Range(1, int.MaxValue)]
//    //[Display(Name = "Max Look Back Bars", Description = "Max Number of Bars to Look Back for calculations", Order = 2, GroupName = "Parameters")]
//    //public int MaxLookBackBars
//    //{ get; set; }

//    //[NinjaScriptProperty]
//    //[Range(1, int.MaxValue)]
//    //[Display(Name = "Pivot Tick Proximity", Description = "Proximity of pivots in Ticks to determine where S/R level will be", Order = 3, GroupName = "Parameters")]
//    //public int PivotTickDiff
//    //{ get; set; }

//    //[NinjaScriptProperty]
//    //[Range(1, int.MaxValue)]
//    //[Display(Name = "ZoneTickSize", Description = "Height in Ticks to draw S/R Zone around S/R level", Order = 4, GroupName = "Parameters")]
//    //public int ZoneTickSize
//    //{ get; set; }

//    //[NinjaScriptProperty]
//    //[Range(1, int.MaxValue)]
//    //[Display(Name = "Max Levels", Description = "Number of S/R Levels To Draw Above and Below The Market", Order = 5, GroupName = "Parameters")]
//    //public int MaxLevels
//    //{ get; set; }

//    //[NinjaScriptProperty]
//    //[Display(Name = "Show Pivots", Order = 6, GroupName = "Parameters")]
//    //public bool ShowPivots
//    //{ get; set; }

//    //[NinjaScriptProperty]
//    //[XmlIgnore]
//    //[Display(Name = "S/R Zone Color", Order = 7, GroupName = "Parameters")]
//    //public Brush ColorBelow
//    //{ get; set; }

//    //[Browsable(false)]
//    //public string ColorBelowSerializable
//    //{
//    //    get { return Serialize.BrushToString(ColorBelow); }
//    //    set { ColorBelow = Serialize.StringToBrush(value); }
//    //}

//    //[NinjaScriptProperty]
//    //[XmlIgnore]
//    //[Display(Name = "Resistance Zone Color", Order = 8, GroupName = "Parameters")]
//    //public Brush ColorAbove
//    //{ get; set; }

//    //[Browsable(false)]
//    //public string ColorAboveSerializable
//    //{
//    //    get { return Serialize.BrushToString(ColorAbove); }
//    //    set { ColorAbove = Serialize.StringToBrush(value); }
//    //}
//    //public double L1SupportPrice
//    //{ get { Update(); return l1SupportPrice; } }
//    //public double L1ResistancePrice
//    //{ get { Update(); return l1ResistancePrice; } }
//    //#endregion

//}


////namespace NinjaTrader.NinjaScript.Indicators
////{
////    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
////    {
////        private DynamicSRLines[] cacheDynamicSRLines;
////        public DynamicSRLines DynamicSRLines(int pivotStrength, int maxLookBackBars, int pivotTickDiff, int zoneTickSize, int maxLevels, bool showPivots, Brush colorBelow, Brush colorAbove)
////        {
////            return DynamicSRLines(Input, pivotStrength, maxLookBackBars, pivotTickDiff, zoneTickSize, maxLevels, showPivots, colorBelow, colorAbove);
////        }

////        public DynamicSRLines DynamicSRLines(ISeries<double> input, int pivotStrength, int maxLookBackBars, int pivotTickDiff, int zoneTickSize, int maxLevels, bool showPivots, Brush colorBelow, Brush colorAbove)
////        {
////            if (cacheDynamicSRLines != null)
////                for (int idx = 0; idx < cacheDynamicSRLines.Length; idx++)
////                    if (cacheDynamicSRLines[idx] != null && cacheDynamicSRLines[idx].PivotStrength == pivotStrength && cacheDynamicSRLines[idx].MaxLookBackBars == maxLookBackBars && cacheDynamicSRLines[idx].PivotTickDiff == pivotTickDiff && cacheDynamicSRLines[idx].ZoneTickSize == zoneTickSize && cacheDynamicSRLines[idx].MaxLevels == maxLevels && cacheDynamicSRLines[idx].ShowPivots == showPivots && cacheDynamicSRLines[idx].ColorBelow == colorBelow && cacheDynamicSRLines[idx].ColorAbove == colorAbove && cacheDynamicSRLines[idx].EqualsInput(input))
////                        return cacheDynamicSRLines[idx];
////            return CacheIndicator<DynamicSRLines>(new DynamicSRLines() { PivotStrength = pivotStrength, MaxLookBackBars = maxLookBackBars, PivotTickDiff = pivotTickDiff, ZoneTickSize = zoneTickSize, MaxLevels = maxLevels, ShowPivots = showPivots, ColorBelow = colorBelow, ColorAbove = colorAbove }, input, ref cacheDynamicSRLines);
////        }
////    }
////}

////namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
////{
////    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
////    {
////        public Indicators.DynamicSRLines DynamicSRLines(int pivotStrength, int maxLookBackBars, int pivotTickDiff, int zoneTickSize, int maxLevels, bool showPivots, Brush colorBelow, Brush colorAbove)
////        {
////            return indicator.DynamicSRLines(Input, pivotStrength, maxLookBackBars, pivotTickDiff, zoneTickSize, maxLevels, showPivots, colorBelow, colorAbove);
////        }

////        public Indicators.DynamicSRLines DynamicSRLines(ISeries<double> input, int pivotStrength, int maxLookBackBars, int pivotTickDiff, int zoneTickSize, int maxLevels, bool showPivots, Brush colorBelow, Brush colorAbove)
////        {
////            return indicator.DynamicSRLines(input, pivotStrength, maxLookBackBars, pivotTickDiff, zoneTickSize, maxLevels, showPivots, colorBelow, colorAbove);
////        }
////    }
////}

////namespace NinjaTrader.NinjaScript.Strategies
////{
////    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
////    {
////        public Indicators.DynamicSRLines DynamicSRLines(int pivotStrength, int maxLookBackBars, int pivotTickDiff, int zoneTickSize, int maxLevels, bool showPivots, Brush colorBelow, Brush colorAbove)
////        {
////            return indicator.DynamicSRLines(Input, pivotStrength, maxLookBackBars, pivotTickDiff, zoneTickSize, maxLevels, showPivots, colorBelow, colorAbove);
////        }

////        public Indicators.DynamicSRLines DynamicSRLines(ISeries<double> input, int pivotStrength, int maxLookBackBars, int pivotTickDiff, int zoneTickSize, int maxLevels, bool showPivots, Brush colorBelow, Brush colorAbove)
////        {
////            return indicator.DynamicSRLines(input, pivotStrength, maxLookBackBars, pivotTickDiff, zoneTickSize, maxLevels, showPivots, colorBelow, colorAbove);
////        }
////    }
////}
