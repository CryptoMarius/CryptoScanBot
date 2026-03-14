using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class Positions
{
    private static void DrawVerticalLine(PlotModel chart, CryptoInterval interval, DateTime? time,
        decimal? atPrice, OxyColor color, CandleTime maxDate, string title, string tag)
    {
        if (time.HasValue && atPrice.HasValue)
        {
            var seriesLevels = new LineSeries { Title = title, Color = color, LineStyle = LineStyle.Dot, StrokeThickness = 1, Font = Const.OxyFontName, Tag = tag, };
            seriesLevels.Points.Add(new DataPoint(CandleTime.FromDateTime(time.Value).Minutes - 1 * interval.Duration, (double)0));
            seriesLevels.Points.Add(new DataPoint(CandleTime.FromDateTime(time.Value).Minutes - 1 * interval.Duration, (double)atPrice.Value));
            chart.Series.Add(seriesLevels);
        }
    }

    private static void DrawHorizontalLine(PlotModel chart, CryptoInterval interval, CryptoPositionStep step,
        decimal atPrice, OxyColor color, CandleTime maxDate, string title, string tag)
    {
        decimal value = atPrice;
        var seriesLevels = new LineSeries { Title = title, Color = color, LineStyle = LineStyle.Dot, StrokeThickness = 1, Font = Const.OxyFontName, Tag = tag, };
        seriesLevels.Points.Add(new DataPoint(CandleTime.FromDateTime(step.CreateTime).Minutes - 1 * interval.Duration, (double)value));
        seriesLevels.Points.Add(new DataPoint(maxDate.Minutes + 2, (double)value));
        chart.Series.Add(seriesLevels);
    }

    private static void DrawSomething(PlotModel chart, CryptoInterval interval, CryptoPositionStep step,
        CandleTime time, ScatterSeries series, OxyColor color, CandleTime maxDate, string tag, bool isFilled)
    {
        decimal value;
        if (isFilled)
            series?.Points.Add(new ScatterPoint(time.Minutes - 1, (double)step.AveragePrice));

        var seriesLevels = new LineSeries { Title = "line", Color = color, LineStyle = LineStyle.Dot, Font = Const.OxyFontName, Tag = tag, };
        if (isFilled)
        {
            value = step.AveragePrice;
            seriesLevels.Points.Add(new DataPoint(time.Minutes - 1 * interval.Duration, (double)value));
            seriesLevels.Points.Add(new DataPoint(time.Minutes + 4 * interval.Duration, (double)value));
        }
        else
        {
            value = step.Price;
            seriesLevels.Points.Add(new DataPoint(CandleTime.FromDateTime(step.CreateTime).Minutes - 1 * interval.Duration, (double)value));
            seriesLevels.Points.Add(new DataPoint(maxDate.Minutes + 2, (double)value));
        }
        chart.Series.Add(seriesLevels);


        if (!isFilled)
        {
            if (step.StopPrice.HasValue)
                DrawHorizontalLine(chart, interval, step, step.StopPrice.Value, OxyColors.Blue, maxDate, "stop", tag);
            if (step.StopLimitPrice.HasValue)
                DrawHorizontalLine(chart, interval, step, step.StopLimitPrice.Value, OxyColors.Blue, maxDate, "limit", tag);
        }
    }


    internal static void Draw(PlotModel chart, List<CryptoPosition> positionList, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, string tag)
    {
        var seriesSell = new ScatterSeries
        {
            Title = "buy",
            MarkerSize = 4,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Diamond,
            Tag = tag
        };

        var seriesBuy = new ScatterSeries
        {
            Title = "sell",
            MarkerSize = 4,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Diamond,
            Tag = tag
        };

        foreach (CryptoPosition position in positionList)
        {
            // Draw a vertical line to mark the beginning of a position
            if (position.Side == CryptoTradeSide.Long)
                DrawVerticalLine(chart, interval, position.CreateTime, position.EntryPrice, OxyColors.Green, maxDate, "start", tag);
            else
                DrawVerticalLine(chart, interval, position.CreateTime, position.EntryPrice, OxyColors.Red, maxDate, "start", tag);

            foreach (CryptoPositionPart positionPart in position.PartList.Values)
            {
                foreach (var step in positionPart.StepList.Values)
                {
                    if (step.Status > CryptoOrderStatus.Filled)
                        continue;

                    bool isFilled = step.Status != CryptoOrderStatus.New;

                    CandleTime time;
                    if (step.CloseTime == null)
                        time = CandleTime.FromDateTime(step.CreateTime);
                    else
                        time = CandleTime.FromDateTime(step.CloseTime!.Value);

                    if (time >= minDate && time <= maxDate)
                    {
                        if (step.Side == CryptoOrderSide.Buy)
                        {
                            DrawSomething(chart, interval, step, time, seriesBuy, OxyColors.Green, maxDate, tag, isFilled);
                        }
                        else
                        {
                            DrawSomething(chart, interval, step, time, seriesSell, OxyColors.Red, maxDate, tag, isFilled);
                        }
                    }
                }
            }
        }

        chart.Series.Add(seriesBuy);
        chart.Series.Add(seriesSell);
    }

}