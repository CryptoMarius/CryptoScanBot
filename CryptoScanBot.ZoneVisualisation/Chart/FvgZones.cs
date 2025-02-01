using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;

namespace CryptoScanBot.ZoneVisualisation.Chart;

public class ChartDrawFvgZones
{
    // todo method also in DrawDlzZones..
    // Introduce a common class?
    private static void DrawZone(PlotModel chart, CryptoZone zone, long minDate, long maxDate)
    {
        // ?? why ??
        //if (zone.Kind == CryptoZoneKind.FairValueGap && !session.ShowFvgZones)
        //    return;

        if (zone.OpenTime >= minDate && zone.OpenTime <= maxDate)
        {
            var colors = Const.ColorList[(zone.Kind, zone.Side, zone.CloseTime.HasValue)];
            OxyColor boxColor = colors.boxColor;
            OxyColor textColor = colors.textColor;


            long dateOpen;
            if (zone.OpenTime != null)
                dateOpen = (long)zone.OpenTime;
            else
                dateOpen = minDate;
            if (zone.Kind == CryptoZoneKind.FairValueGap)
                dateOpen -= 3 * zone.Interval.Duration; // this looks better

            long dateLast;
            if (zone.CloseTime != null)
                dateLast = (long)zone.CloseTime;
            else
                dateLast = maxDate + 10000;

            OxyColor col;
            if (GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply)
            {
                if (zone.Strength == CryptoZoneStrength.Strong)
                    col = OxyColor.FromArgb(128, boxColor.R, boxColor.G, boxColor.B);
                else
                    col = OxyColor.FromArgb(64, boxColor.R, boxColor.G, boxColor.B);
            }
            else col = OxyColor.FromArgb(128, boxColor.R, boxColor.G, boxColor.B);


            int stroke = 0;
            //if (zone.CloseTime != null)
            //    stroke = 1;

            // Create a rectangle annotation
            var rectangle = new RectangleAnnotation
            {
                MinimumX = dateOpen,  // X-coordinate of the lower-left corner
                MinimumY = (double)zone.Bottom,  // Y-coordinate of the lower-left corner
                MaximumX = dateLast,  // X-coordinate of the upper-right corner
                MaximumY = (double)zone.Top,  // Y-coordinate of the upper-right corner
                Fill = col, //OxyColor.FromArgb(128, boxColor.R, boxColor.G, boxColor.B),
                Stroke = OxyColor.FromArgb(128 + 64 + 32 + 16 + 8 + 4 + 2, boxColor.R, boxColor.G, boxColor.B), // rectangle
                StrokeThickness = stroke, // Border thickness
                TextColor = textColor,
                Text = zone.Description,
                ToolTip = zone.Description, // does not work, weak..
            };
            chart.Annotations.Add(rectangle);
        }
    }


    private static bool RecentlyClosed(CryptoZone zone, CryptoInterval interval)
    {
        if (zone.CloseTime != null)
        {
            long allowedTime = interval.Duration * 60; // 60 candles?
            long currentTime = CandleTools.GetUnixTime(DateTime.UtcNow, 60);

            if (zone.CloseTime > currentTime - allowedTime)
                return true;
        }
        return false;
    }


    public static void Draw(PlotModel chart, CryptoSymbol symbol, long minDate, long maxDate)
    {
        var symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
            {
                var symbolDataInterval = symbolData.GetAccountSymbolInterval(interval.IntervalPeriod);
                foreach (var zone in symbolDataInterval.FvgZones.LongOpen)
                    DrawZone(chart, zone, minDate, maxDate);
                foreach (var zone in symbolDataInterval.FvgZones.ShortOpen)
                    DrawZone(chart, zone, minDate, maxDate);

                foreach (var zone in symbolDataInterval.FvgZones.LongClosed)
                {
                    if (RecentlyClosed(zone, interval))
                        DrawZone(chart, zone, minDate, maxDate);
                }
                foreach (var zone in symbolDataInterval.FvgZones.ShortClosed)
                {
                    if (RecentlyClosed(zone, interval))
                        DrawZone(chart, zone, minDate, maxDate);
                }
            }
        }
    }

}
