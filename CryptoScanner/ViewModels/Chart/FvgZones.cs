using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;

namespace CryptoScanner.ViewModels.Chart;

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
            OxyColor boxColor = colors.boxColor;
            OxyColor textColor = colors.textColor;


            CandleTime dateOpen;
            if (zone.OpenTime != 0)
                dateOpen = zone.OpenTime;
            else
                dateOpen = minDate;
            if (zone.Kind == CryptoZoneKind.FairValueGap)
                dateOpen -= 3 * zone.Interval.Duration; // this looks better

            CandleTime dateLast;
            if (zone.CloseTime != null)
                dateLast = zone.CloseTime.Value;
            else
                dateLast = maxDate + 25;

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
                Layer = AnnotationLayer.BelowSeries,
                MinimumX = dateOpen.Minutes,  // X-coordinate of the lower-left corner
                MinimumY = (double)zone.Bottom,  // Y-coordinate of the lower-left corner
                MaximumX = dateLast.Minutes,  // X-coordinate of the upper-right corner
                MaximumY = (double)zone.Top,  // Y-coordinate of the upper-right corner
                Fill = col, //OxyColor.FromArgb(128, boxColor.R, boxColor.G, boxColor.B),
                Stroke = OxyColor.FromArgb(128 + 64 + 32 + 16 + 8 + 4 + 2, boxColor.R, boxColor.G, boxColor.B), // rectangle
                StrokeThickness = stroke, // Border thickness
                TextColor = textColor,
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
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
            {
                var symbolDataInterval = symbolData.Get(interval.IntervalPeriod);
                foreach (var zone in symbolDataInterval.FvgZones.LongOpen)
                    DrawZone(chart, zone, minDate, maxDate, group);
                foreach (var zone in symbolDataInterval.FvgZones.ShortOpen)
                    DrawZone(chart, zone, minDate, maxDate, group);

                foreach (var zone in symbolDataInterval.FvgZones.LongClosed)
                {
                    if (RecentlyClosed(zone, interval))
                        DrawZone(chart, zone, minDate, maxDate, group);
                }
                foreach (var zone in symbolDataInterval.FvgZones.ShortClosed)
                {
                    if (RecentlyClosed(zone, interval))
                        DrawZone(chart, zone, minDate, maxDate, group);
                }
            }
        }
    }

}
