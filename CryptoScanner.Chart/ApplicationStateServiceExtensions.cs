using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

using CryptoScanner.Core.Services;

namespace CryptoScanner.Chart.Services;

/// <summary>
/// Avalonia-specific extension methods for ApplicationStateService (Chart project copy).
/// </summary>
public static class ApplicationStateServiceExtensions
{
    public static void SaveWindowState(this ApplicationStateService service, string windowName, Window window)
    {
        service.SaveWindowStateValues(windowName,
            window.Position.X, window.Position.Y,
            window.Width, window.Height,
            window.WindowState.ToString());

        service.FlushToDisk();
        service.FlushWindowStateToDisk();
    }

    public static void RestoreWindowState(this ApplicationStateService service, string windowName, Window window)
    {
        var state = service.GetOrCreateWindowState(windowName);
        if (string.IsNullOrEmpty(state.State))
            return;

        if (Enum.TryParse<Avalonia.Controls.WindowState>(state.State, out var windowState))
        {
            Screen? targetScreen;
            if (IsPositionOnScreen(window, state.X, state.Y, out targetScreen))
            {
                window.Position = new PixelPoint((int)state.X, (int)state.Y);
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                targetScreen = window.Screens.Primary ?? window.Screens.All.FirstOrDefault();
            }

            double width = state.Width;
            double height = state.Height;
            if (targetScreen != null)
            {
                double scaling = targetScreen.Scaling > 0 ? targetScreen.Scaling : 1.0;
                width = Math.Min(width, targetScreen.WorkingArea.Width / scaling);
                height = Math.Min(height, targetScreen.WorkingArea.Height / scaling);
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
