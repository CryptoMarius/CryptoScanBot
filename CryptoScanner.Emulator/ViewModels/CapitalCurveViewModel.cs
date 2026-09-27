using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Chart.ViewModels.Chart;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Trader;

using Dapper;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// The capital per day of the last finished run (open point 53): one point on the start day with
/// the start capital, one per replayed day, and a last point with the end result, read from the
/// AssetSnapshot rows the run wrote. Shows whether a run grew steadily or lost it all in one day,
/// which the single result figure in the grid cannot tell.
/// </summary>
public partial class CapitalCurveViewModel : ObservableObject
{
    [ObservableProperty]
    private PlotModel? _model;

    [ObservableProperty]
    private string _caption = "";


    /// <summary>Load the curve of the most recently started run that has finished.</summary>
    [RelayCommand]
    public void ShowLatest()
    {
        try
        {
            using var database = new CryptoDatabase();
            database.Open();
            var run = database.Connection.QueryFirstOrDefault<(int Id, string? Label)>(
                "select Id, Label from EmulatorRun where FinishedAt is not null order by StartedAt desc limit 1");
            if (run.Id == 0)
            {
                Caption = "No finished run yet.";
                Model = null;
                return;
            }
            Show(run.Id, run.Label ?? "");
        }
        catch (Exception ex)
        {
            Caption = $"Failed to load the capital curve: {ex.Message}";
            Model = null;
        }
    }


    private void Show(int runId, string label)
    {
        List<AssetSnapshotTools.AssetSnapshotDay> days = AssetSnapshotTools.LoadDailyTotals(runId);
        if (days.Count == 0)
        {
            Caption = $"Run #{runId} {label}: no capital snapshots (a run from before 30-08-2026, or without asset management).";
            Model = null;
            return;
        }

        decimal start = days[0].Value;
        decimal end = days[^1].Value;
        decimal change = start != 0 ? 100m * (end - start) / start : 0m;
        Caption = $"Run #{runId} {label}: {start:N2} to {end:N2} ({change:+0.00;-0.00}%) over {days.Count} day(s)";

        // Larger fonts than the OxyPlot default: this is read at a glance from across the desk.
        var model = new PlotModel
        {
            TextColor = Const.ChartTextColor,
            Background = Const.ChartSurfaceColor,
            DefaultFontSize = 14,
            TitleFontSize = 16,
            Title = "Capital per day",
        };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd-MM",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(80, 255, 255, 255),
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            // Not from zero: the movement on top of the start capital is the interesting part
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(80, 255, 255, 255),
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
            StringFormat = "N0",
        });

        var series = new LineSeries
        {
            Title = "Capital",
            Color = OxyColors.DodgerBlue,
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = OxyColors.DodgerBlue,
            TrackerFormatString = "{2:dd-MM-yyyy}\n{4:N2}",
        };
        foreach (AssetSnapshotTools.AssetSnapshotDay day in days)
            series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(day.Date), (double)day.Value));
        model.Series.Add(series);

        // The start capital as a line, so a glance says whether the run is above or below it
        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = (double)start,
            Color = OxyColors.White,
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
        });

        Model = model;
    }
}
