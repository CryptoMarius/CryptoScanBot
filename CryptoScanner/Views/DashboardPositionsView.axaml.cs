using Avalonia.Controls;

using OxyPlot;

namespace CryptoScanner.Views;

public partial class DashboardPositionsView : UserControl
{
    public DashboardPositionsView()
    {
        InitializeComponent();

        //// Test?
        //// Change behaviour
        //PlotViewControl1.AttachedToVisualTree += (_, _) =>
        //{
        //    if (PlotViewControl1.ActualController is { } controller)
        //    {
        //        //controller.UnbindAll(); // leave the original intact, we just need to tweak it a bit
        //        controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
        //        controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control, PlotCommands.ZoomRectangle);
        //        controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt, 2, PlotCommands.ResetAt);
        //        controller.UnbindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Shift);
        //        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.ZoomRectangle);
        //        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control, 2, PlotCommands.ResetAt);
        //        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Alt, PlotCommands.PanAt);
        //        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Shift, PlotCommands.SnapTrack);
        //    }
        //};

    }
}
