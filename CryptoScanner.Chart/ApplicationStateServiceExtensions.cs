using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

using CryptoScanner.Core.Services;

namespace CryptoScanner.Chart.Services;

/// <summary>
/// Avalonia-specific extension methods for ApplicationStateService (Chart project copy).
/// </summary>
public static class ApplicationStateServiceExtensions
{
    public static void SaveWindowState(this ApplicationStateService service, string windowName, Window window)
    {
        var windowState = window.WindowState;

        // Only a normal window reports its own rectangle; a maximized one reports the maximized
        // rectangle (-8,-8 and 16 pixels wider than the screen on Windows). Saving that as the
        // window bounds made the restored "normal" window as large as the screen, so restoring
        // from maximized looked like nothing happened and the window could not be moved.
        if (windowState == Avalonia.Controls.WindowState.Normal)
            CaptureNormalBounds(service, windowName, window);

        // A window closed while minimized would otherwise come back minimized, and invisible.
        if (windowState == Avalonia.Controls.WindowState.Minimized)
            windowState = Avalonia.Controls.WindowState.Normal;

        service.SaveWindowStateName(windowName, windowState.ToString());
        service.FlushToDisk();
        service.FlushWindowStateToDisk();
    }

    /// <summary>
    /// Keep the last normal position and size of the window up to date while it is open, so the
    /// rectangle from before a maximize survives a restart. Call once, after RestoreWindowState.
    /// </summary>
    public static void TrackWindowState(this ApplicationStateService service, string windowName, Window window)
    {
        window.PositionChanged += (_, _) => CaptureNormalBoundsDeferred(service, windowName, window);
        window.SizeChanged += (_, _) => CaptureNormalBoundsDeferred(service, windowName, window);
    }

    private static void CaptureNormalBoundsDeferred(ApplicationStateService service, string windowName, Window window)
    {
        // On a maximize Windows first moves and resizes the window and only then does Avalonia
        // update WindowState, so at the moment of the event the state still reads Normal while
        // the position is already the maximized one. Posting the capture lets the state change
        // land first, and the check below then skips the maximized rectangle.
        Dispatcher.UIThread.Post(() =>
        {
            if (window.WindowState != Avalonia.Controls.WindowState.Normal || !window.IsVisible)
                return;
            CaptureNormalBounds(service, windowName, window);
        }, DispatcherPriority.Background);
    }

    private static void CaptureNormalBounds(ApplicationStateService service, string windowName, Window window)
    {
        double width = window.ClientSize.Width;
        double height = window.ClientSize.Height;
        if (width <= 0 || height <= 0)
            return;

        service.SaveWindowNormalBounds(windowName, window.Position.X, window.Position.Y, width, height);
    }

    public static void RestoreWindowState(this ApplicationStateService service, string windowName, Window window)
    {
        var state = service.GetOrCreateWindowState(windowName);
        if (string.IsNullOrEmpty(state.State))
            return;

        if (Enum.TryParse<Avalonia.Controls.WindowState>(state.State, out var windowState))
        {
            Screen? targetScreen;
            double fillsScreenFactor = 1.0;
            if (IsPositionOnScreen(window, state.X, state.Y, out targetScreen))
            {
                window.Position = new PixelPoint((int)state.X, (int)state.Y);
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                targetScreen = window.Screens.Primary ?? window.Screens.All.FirstOrDefault();
                // Either the screen is gone or this is an entry written by the old code, which
                // stored the maximized rectangle (-8,-8 and larger than the screen). Capping that
                // at the working area gave a normal window that filled the screen, so restoring
                // from maximized changed nothing visible. Leave some margin instead.
                fillsScreenFactor = 0.9;
            }

            double width = state.Width;
            double height = state.Height;
            if (targetScreen != null)
            {
                double scaling = targetScreen.Scaling > 0 ? targetScreen.Scaling : 1.0;
                width = Math.Min(width, fillsScreenFactor * targetScreen.WorkingArea.Width / scaling);
                height = Math.Min(height, fillsScreenFactor * targetScreen.WorkingArea.Height / scaling);
            }

            window.Width = width;
            window.Height = height;
            window.WindowState = windowState;
        }
    }

    private static bool IsPositionOnScreen(Window window, double x, double y, out Screen? matchedScreen)
    {
        matchedScreen = null;
        try
        {
            var point = new PixelPoint((int)x, (int)y);
            var screens = window.Screens.All;

            foreach (var screen in screens)
            {
                if (screen.WorkingArea.Contains(point))
                {
                    matchedScreen = screen;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
