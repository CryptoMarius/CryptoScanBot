using Avalonia.Controls;
using Avalonia.Threading;

using CryptoScanner.Visualisation.ViewModels;

using OxyPlot.Series;

namespace CryptoScanner.Visualisation.Views;

public partial class VisualisationWindow : Window
{
    public VisualisationWindow()
    {
        InitializeComponent();

        this.Loaded += OnWindowLoaded;

        this.Loaded += (s, e) =>
        {
            var plotView = this.FindControl<OxyPlot.Avalonia.PlotView>("PlotViewControl");

            if (plotView != null && DataContext is VisualisationViewModel vm)
            {
                plotView.Model = vm.PlotModel;

                // ✓ FORCE RESIZE (workaround voor rendering bug)
                plotView.InvalidatePlot(true);
                plotView.InvalidateVisual();
                plotView.InvalidateMeasure();

                // ✓ Delay render (hack)
                Dispatcher.UIThread.Post(() =>
                {
                    plotView.InvalidatePlot(true);
                }, DispatcherPriority.Render);
            }
        };

        this.Loaded += (s, e) =>
        {
            var plotView = this.FindControl<OxyPlot.Avalonia.PlotView>("PlotViewControl");

            // ✓ CHECK SIZE:
            System.Diagnostics.Debug.WriteLine($"PlotView.Bounds: {plotView?.Bounds}");
            System.Diagnostics.Debug.WriteLine($"PlotView.Width: {plotView?.Width}");
            System.Diagnostics.Debug.WriteLine($"PlotView.Height: {plotView?.Height}");
            System.Diagnostics.Debug.WriteLine($"Window.Width: {this.Width}");
            System.Diagnostics.Debug.WriteLine($"Window.Height: {this.Height}");

            if (plotView != null && DataContext is VisualisationViewModel vm)
            {
                plotView.Model = vm.PlotModel;
                plotView.InvalidatePlot(true);

                // ✓ CHECK AXES RANGE:
                var model = vm.PlotModel;
                System.Diagnostics.Debug.WriteLine($"Axes count: {model.Axes.Count}");
                if (model.Axes.Count >= 2)
                {
                    System.Diagnostics.Debug.WriteLine($"X-Axis: Min={model.Axes[0].ActualMinimum}, Max={model.Axes[0].ActualMaximum}");
                    System.Diagnostics.Debug.WriteLine($"Y-Axis: Min={model.Axes[1].ActualMinimum}, Max={model.Axes[1].ActualMaximum}");
                }

                // ✓ CHECK SERIES DATA:
                if (model.Series.Count > 0 && model.Series[0] is LineSeries line)
                {
                    System.Diagnostics.Debug.WriteLine($"LineSeries points: {line.Points.Count}");
                    if (line.Points.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"First point: X={line.Points[0].X}, Y={line.Points[0].Y}");
                        System.Diagnostics.Debug.WriteLine($"Last point: X={line.Points[^1].X}, Y={line.Points[^1].Y}");
                    }
                }
            }
        };
        DataContext = new VisualisationViewModel();
    }

    private void OnWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var border = this.FindControl<Border>("ChartBorder");
        if (border == null) return;

        var plotView = new OxyPlot.Avalonia.PlotView
        {
            Background = Avalonia.Media.Brushes.Yellow,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };

        if (DataContext is VisualisationViewModel vm)
        {
            plotView.Model = vm.PlotModel;
            System.Diagnostics.Debug.WriteLine($"PlotModel assigned - Series: {vm.PlotModel.Series.Count}");
        }

        border.Child = plotView;

        // ✓ MULTIPLE render passes (OxyPlot bug workaround)
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            System.Diagnostics.Debug.WriteLine($"Pass 1 - PlotView.Bounds: {plotView.Bounds}");
            System.Diagnostics.Debug.WriteLine($"Pass 1 - PlotView.ActualModel: {plotView.ActualModel != null}");

            plotView.InvalidatePlot(true);
            plotView.InvalidateVisual();
            plotView.InvalidateMeasure();

            // ✓ SECOND pass
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Pass 2 - Forcing render again");
                plotView.InvalidatePlot(true);

                // ✓ THIRD pass (nuclear option)
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"Pass 3 - Final render");

                    // ✓ Reassign model (force complete refresh)
                    var model = plotView.Model;
                    plotView.Model = null;
                    plotView.InvalidatePlot(true);
                    plotView.Model = model;
                    plotView.InvalidatePlot(true);

                }, Avalonia.Threading.DispatcherPriority.Loaded);
            }, Avalonia.Threading.DispatcherPriority.Render);
        }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is VisualisationViewModel vm)
        {
            vm.OnClosing();
        }
        
        base.OnClosing(e);
    }


    private void OnPaintSurface(object? sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
    {
        if (DataContext is not VisualisationViewModel vm) return;

        var surface = e.Surface;
        var canvas = surface.Canvas;
        var info = e.Info;

        canvas.Clear(SkiaSharp.SKColors.White);

        // Render OxyPlot to SkiaSharp
        using var rc = new OxyPlot.SkiaSharp.SkiaRenderContext
        {
            SkCanvas = canvas,
            RenderTarget = OxyPlot.SkiaSharp.RenderTarget.PixelGraphic
        };

        vm.PlotModel.Update(true);
        vm.PlotModel.Render(rc, new OxyRect(0, 0, info.Width, info.Height));
    }
}
