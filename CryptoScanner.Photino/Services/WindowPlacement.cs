using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

using Photino.NET;

namespace CryptoScanner.Photino.Services;

/// <summary>
/// Remembers where a secondary window (the chart, TradingView) sits: its position, its size, the
/// screen it is on and whether it was maximized. The screen is not stored separately: the position
/// is a point on the whole desktop, so restoring it puts the window back on the same monitor.
/// <para>
/// Uses the same WindowStates entries as the main window and the Avalonia host
/// (CryptoScanBot-window.json in the data folder). The normal rectangle is captured on every move
/// and resize, because Photino only reports the maximized rectangle for a maximized window and
/// exposes no restore rectangle. The file itself is written when the window closes, and again by
/// the shutdown of the application.
/// </para>
/// <para>
/// Everything here runs on the thread that owns the windows: Photino only reports its geometry
/// there.
/// </para>
/// </summary>
internal sealed class WindowPlacement
{
    private readonly PhotinoWindow _window;
    private readonly string _windowName;
    private readonly ApplicationStateService _stateService;
    private readonly bool _restored;

    /// <summary>
    /// Apply the saved placement (or the default size, centered) to a window that does not exist
    /// natively yet, and start tracking it. Call <see cref="AfterCreate"/> once the native window
    /// exists and <see cref="Save"/> when it closes.
    /// </summary>
    public WindowPlacement(PhotinoWindow window, string windowName, ApplicationStateService stateService,
        int defaultWidth, int defaultHeight)
    {
        _window = window;
        _windowName = windowName;
        _stateService = stateService;

        //// Both default to true, and while they are on Photino ignores SetSize / SetLeft / SetTop
        //_window
        //    .SetUseOsDefaultSize(false)
        //    .SetUseOsDefaultLocation(false);
        //
        //var state = _stateService.GetOrCreateWindowState(_windowName);
        //_restored = state.Width > 0 && state.Height > 0;
        //if (_restored)
        //{
        //    _window
        //        .SetSize((int)state.Width, (int)state.Height)
        //        .SetLeft((int)state.X)
        //        .SetTop((int)state.Y);
        //
        //    // Maximizing after the normal rectangle has been set maximizes on the monitor that
        //    // rectangle is on, so a window maximized on the second screen comes back there
        //    if (state.State == "Maximized")
        //        _window.SetMaximized(true);
        //}
        //else
        //    _window.SetSize(defaultWidth, defaultHeight).Center();
        //
        //_window.RegisterLocationChangedHandler((_, _) => Capture());
        //_window.RegisterSizeChangedHandler((_, _) => Capture());

        // Since 07-11-2026 the window is created the way the TradingView window always was (OS
        // default place, default size) and the saved placement is applied in AfterCreate, on the
        // native window. (This was first suspected of leaving both secondary windows empty; the
        // real cause was that they asked for other browser switches than the main window, see
        // WindowChrome.BrowserArguments.)
        _window
            .SetUseOsDefaultSize(false)
            .SetSize(defaultWidth, defaultHeight)
            .SetUseOsDefaultLocation(true);

        var state = _stateService.GetOrCreateWindowState(_windowName);
        _restored = state.Width > 0 && state.Height > 0;
    }

    /// <summary>
    /// Put the window back on a monitor that actually exists. Photino can only list the monitors
    /// once the native window has been created, so this has to wait until then. The saved position
    /// is kept when it falls inside one of the attached monitors; when that screen is gone the
    /// window is centered on the main monitor. The size is capped at the monitor it ends up on.
    /// </summary>
    public void AfterCreate()
    {
        // Tracked from here on: during the creation itself the window reports the OS default place
        try
        {
            if (_restored)
                RestoreOnNativeWindow();
        }
        finally
        {
            _window.RegisterLocationChangedHandler((_, _) => Capture());
            _window.RegisterSizeChangedHandler((_, _) => Capture());
        }
    }

    private void RestoreOnNativeWindow()
    {
        try
        {
            var state = _stateService.GetOrCreateWindowState(_windowName);
            int x = (int)state.X;
            int y = (int)state.Y;

            var monitors = _window.Monitors.ToList();
            if (monitors.Count == 0)
                return;

            int index = monitors.FindIndex(monitor =>
                x >= monitor.MonitorArea.X && x < monitor.MonitorArea.X + monitor.MonitorArea.Width &&
                y >= monitor.MonitorArea.Y && y < monitor.MonitorArea.Y + monitor.MonitorArea.Height);
            if (index >= 0)
            {
                var monitor = monitors[index];
                int width = Math.Min((int)state.Width, monitor.MonitorArea.Width);
                int height = Math.Min((int)state.Height, monitor.MonitorArea.Height);
                //if (width != (int)state.Width || height != (int)state.Height)
                //{
                //    if (_window.Maximized)
                //        return;
                //    _window.SetSize(width, height);
                //}
                _window.SetSize(width, height);
                _window.SetLeft(x);
                _window.SetTop(y);

                // Maximizing after the normal rectangle has been set maximizes on the monitor that
                // rectangle is on, so a window maximized on the second screen comes back there
                if (state.State == "Maximized")
                    _window.SetMaximized(true);
                return;
            }

            // The screen the window was on is no longer connected
            var target = _window.MainMonitor;
            bool wasMaximized = state.State == "Maximized";
            if (wasMaximized)
                _window.SetMaximized(false);
            _window.SetSize(
                Math.Min((int)state.Width, (int)(0.9 * target.MonitorArea.Width)),
                Math.Min((int)state.Height, (int)(0.9 * target.MonitorArea.Height)));
            _window.Center();
            if (wasMaximized)
                _window.SetMaximized(true);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, $"WindowPlacement.AfterCreate({_windowName})");
        }
    }

    /// <summary>Store the placement and write the window file. Call from the closing handler.</summary>
    public void Save()
    {
        try
        {
            Capture();
            _stateService.FlushWindowStateToDisk();
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, $"WindowPlacement.Save({_windowName})");
        }
    }

    /// <summary>
    /// Keep the in-memory entry current. A maximized window only updates its state, so the normal
    /// rectangle from before the maximize is what comes back after un-maximizing. A minimized window
    /// changes nothing: it would otherwise come back minimized, and invisible.
    /// </summary>
    private void Capture()
    {
        try
        {
            if (_window.Minimized)
                return;

            if (_window.Maximized)
            {
                _stateService.SaveWindowStateName(_windowName, "Maximized");
                return;
            }

            int width = _window.Width;
            int height = _window.Height;
            if (width <= 0 || height <= 0)
                return;

            _stateService.SaveWindowNormalBounds(_windowName, _window.Left, _window.Top, width, height);
            _stateService.SaveWindowStateName(_windowName, "Normal");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, $"WindowPlacement.Capture({_windowName})");
        }
    }
}
