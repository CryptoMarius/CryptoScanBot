using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Analyzers.Chart;
using CryptoScanner.Core.Settings;

using OxyPlot;
using OxyPlot.Annotations;

namespace CryptoScanner.Chart.ViewModels.Chart;

public class FvgZones
{
    // todo method also in DrawDlzZones..
    // Introduce a common class?
    private static void DrawZone(PlotModel chart, CryptoZone zone, CandleTime minDate, CandleTime maxDate, string group)
    {
        // ?? why ??
        //if (zone.Kind == CryptoZoneKind.FairValueGap && !session.ShowFvgZones)
        //    return;

        if (zone.OpenTime <= maxDate) //zone.OpenTime >= minDate && 
        {
            var colors = Const.ColorList[(zone.Kind, zone.Side, zone.CloseTime.HasValue)];
            //OxyColor boxColor = colors.boxColor;
            // The colour of the zone gear since 07-11-2026 (dlzLong, fvgShort, ...); Const.ColorList is its default
            OxyColor boxColor = ChartStyleOxy.ColorFor(ChartStyleSettings.ZoneStyleKey(zone.Kind, zone.Side), colors.boxColor);
            OxyColor textColor = colors.textColor;


            CandleTime dateOpen;
            if (zone.OpenTime != 0)
                dateOpen = zone.OpenTime;
            else
                dateOpen = minDate;
            if (zone.OpenTime < minDate)
                dateOpen = minDate;
            //if (zone.Kind == CryptoZoneKind.FairValueGap)
            //    dateOpen -= 3 * zone.Interval.Duration; // this looks better
            // The block starts where the zone starts, as in the Photino chart (06-11-2026)

            CandleTime dateLast;
            if (zone.CloseTime != null)
                dateLast = zone.CloseTime.Value;
            else
                dateLast = maxDate + 25;

            OxyColor col;
            if (GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply)
            {
                if (zone.Strength == CryptoZoneStrength.Strong)
                    col = ChartStyleOxy.WithAlpha(boxColor, 128);
                else
                    col = ChartStyleOxy.WithAlpha(boxColor, 64);
            }
            else col = ChartStyleOxy.WithAlpha(boxColor, 128);

            // The FVG blocks are coloured per side now (green bull, red bear) and many of them overlap,
            // so they stay quite transparent whatever the strength setting says: the text on them and
            // the candles behind them must stay readable. 96 of 255 is the 0.38 the Photino chart uses.
            if (zone.Kind == CryptoZoneKind.FairValueGap)
                col = ChartStyleOxy.WithAlpha(boxColor, 96);


            int stroke = 0;
            //if (zone.CloseTime != null)
            //    stroke = 1;

            // Create a rectangle annotation
            var rectangle = new RectangleAnnotation
            {
                Layer = AnnotationLayer.BelowSeries,
                MinimumX = dateOpen.Minutes,  // X-coordinate of the lower-left corner
                MinimumY = (double)zone.Bottom,  // Y-coordinate of the lower-left corner
                MaximumX = dateLast.Minutes,  // X-coordinate of the upper-right corner
                MaximumY = (double)zone.Top,  // Y-coordinate of the upper-right corner
                Fill = col, //OxyColor.FromArgb(128, boxColor.R, boxColor.G, boxColor.B),
                Stroke = ChartStyleOxy.WithAlpha(boxColor, 128 + 64 + 32 + 16 + 8 + 4 + 2), // rectangle
                StrokeThickness = stroke, // Border thickness
                TextColor = textColor,
                // Pin to the price axis explicitly — see SmcZones for the rationale.
                YAxisKey = "price",
                Text = zone.Description,
                //Text = zone.Id.ToString() + " " + zone.Description,
                ToolTip = zone.Description, // does not work, weak..
                Tag = group,
            };
            chart.Annotations.Add(rectangle);
        }
    }


    private static bool RecentlyClosed(CryptoZone zone, CryptoInterval interval)
    {
        if (zone.CloseTime != null)
        {
            uint allowedTime = interval.Duration * 250; // 60 candles?
            CandleTime currentTime = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1);

            //DateTime currentDate = CandleTools.GetUnixDate(currentTime);
            //DateTime closeDate = CandleTools.GetUnixDate(zone.CloseTime);

            if (zone.CloseTime > currentTime - allowedTime)
                return true;
        }
        return false;
    }


    public static void Draw(PlotModel chart, CryptoSymbol symbol, CandleTime minDate, CandleTime maxDate, string group)
    {
        var symbolData = symbol.Data;
        // The FVG intervals, not the DLZ ones: FVG zones on 4h/1d were never drawn otherwise
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
            {
                var symbolDataInterval = symbolData.Get(interval.IntervalPeriod);
                foreach (var zone in symbolDataInterval.Fvg.Zones.LongOpen)
                    DrawZone(chart, zone, minDate, maxDate, group);
                foreach (var zone in symbolDataInterval.Fvg.Zones.ShortOpen)
                    DrawZone(chart, zone, minDate, maxDate, group);

                //foreach (var zone in symbolDataInterval.Fvg.Zones.LongClosed)
                //{
                //    if (RecentlyClosed(zone, interval))
                //        DrawZone(chart, zone, minDate, maxDate, group);
                //}
                //foreach (var zone in symbolDataInterval.Fvg.Zones.ShortClosed)
                //{
                //    if (RecentlyClosed(zone, interval))
                //        DrawZone(chart, zone, minDate, maxDate, group);
                //}
                // A closed zone is drawn when it closed inside the chart's window, the rule of the
                // Photino chart. RecentlyClosed measured against the pc clock (250 candles back), so in
                // a position window of last month or in the emulator every closed zone disappeared.
                foreach (var zone in symbolDataInterval.Fvg.Zones.LongClosed)
                {
                    if (zone.CloseTime >= minDate)
                        DrawZone(chart, zone, minDate, maxDate, group);
                }
                foreach (var zone in symbolDataInterval.Fvg.Zones.ShortClosed)
                {
                    if (zone.CloseTime >= minDate)
                        DrawZone(chart, zone, minDate, maxDate, group);
                }
            }
        }
    }

}
