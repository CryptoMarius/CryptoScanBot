using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Services;
using CryptoScanner.UI.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using Photino.Blazor;
using Photino.NET;

namespace CryptoScanner.Photino.Services;

/// <summary>
/// The chart in a window of its own, the way the Avalonia scanner shows it, so it can sit on a
/// screen of its own next to the scanner and TradingView.
/// <para>
/// Unlike the TradingView window this one shows a page of the application itself, so it needs a
/// Blazor view of its own: a second <see cref="PhotinoWebViewManager"/> on a second
/// <see cref="PhotinoWindow"/>, with <see cref="ChartWindowRoot"/> as its only root component.
/// PhotinoBlazorApp only builds one for the main window, but the class is public and takes the
/// window it serves. Both views share the service provider, so the singletons (SymbolService,
/// DashboardService, ...) are the same objects and the chart follows the selected symbol by
/// itself. Each view gets its own service scope, so the scoped services are its own.
/// </para>
/// <para>
/// Everything happens on the thread that owns the main window, for the same reason as in
/// <see cref="TradingViewWindow"/>: Photino runs one message loop, started by the first window.
/// </para>
/// </summary>
public sealed class ChartWindow
{
    /// <summary>The same entry the Avalonia chart window uses in the window file.</summary>
    private const string WindowName = "ChartWindow";

    private readonly PhotinoBlazorApp _app;
    private readonly ApplicationStateService _stateService;

    /// <summary>Only ever touched on the main window's thread.</summary>
    private PhotinoWindow? _window;
    private PhotinoWebViewManager? _manager;
    private WindowPlacement? _placement;

    public ChartWindow(PhotinoBlazorApp app, ApplicationStateService stateService)
    {
        _app = app;
        _stateService = stateService;

        // The title bar is drawn by the operating system and has to be told about a theme switch
        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (_, _) =>
        {
            try
            {
                _app.MainWindow.Invoke(() => WindowChrome.ApplyTitleBarTheme(_window));
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "ChartWindow.ThemeChanged");
            }
        });
    }

    /// <summary>Show the window, or bring the open one to the front. Safe to call from any thread.</summary>
    public void Show()
    {
        try
        {
            _app.MainWindow.Invoke(ShowOnMainThread);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ChartWindow.Show");
            GlobalData.AddTextToLogTab("Could not open the chart window: " + error.Message);
        }
    }

    /// <summary>Close the window if it is open. Called during shutdown.</summary>
    public void Close()
    {
        try
        {
            _app.MainWindow.Invoke(() =>
            {
                try { _window?.Close(); }
                catch { /* already gone */ }
            });
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ChartWindow.Close");
        }
    }

    private void ShowOnMainThread()
    {
        try
        {
            if (_window != null)
            {
                // Photino has no "activate": un-minimize, and toggle topmost to pull the window in
                // front of the main window the click came from
                //_window.SetMinimized(false);
                //_window.SetTopMost(true);
                //_window.SetTopMost(false);
                // The topmost toggle left a window behind another one where it was
                WindowChrome.BringToFront(_window);
                return;
            }

            // The start of the clock for "Chart page created" and "Chart timing" in the log
            ScannerLog.Logger.Info("Chart window opening");

            var services = _app.Services;
            var window = new PhotinoWindow()
                .SetTitle($"Chart ({GlobalData.Settings.General.ExchangeName})")
                .ApplyIcon()
                // The same browser switches as the main window, or this window gets no browser
                .ApplyBrowserArguments()
                .SetResizable(true);
            _placement = new WindowPlacement(window, WindowName, _stateService, 1400, 900);

            // A Blazor view of its own on this window. Same file provider and options as the main
            // view, so it serves the same index.html and the same scripts and styles; its own
            // JS component store, because that store belongs to one view.
            var manager = new PhotinoWebViewManager(window, services,
                services.GetRequiredService<Dispatcher>(),
                services.GetRequiredService<IFileProvider>(),
                new JSComponentConfigurationStore(),
                services.GetRequiredService<IOptions<PhotinoBlazorAppConfiguration>>());
            window.RegisterCustomSchemeHandler(PhotinoWebViewManager.BlazorAppScheme, manager.HandleWebRequest);

            //window.RegisterWindowClosingHandler((sender, e) =>
            //{
            //    _placement?.Save();
            //    _placement = null;
            //    _window = null;
            //
            //    var closingManager = _manager;
            //    _manager = null;
            //    if (closingManager != null)
            //        _ = DisposeManagerAsync(closingManager);
            //
            //    return false; // false = do not cancel, let it close
            //});
            window.RegisterWindowClosingHandler((sender, e) =>
            {
                // First close: hold the window open until the chart page is gone, see
                // CloseAfterTeardownAsync. That calls Close again, and then it really closes.
                var closingManager = _manager;
                if (closingManager != null)
                {
                    _manager = null;
                    _ = CloseAfterTeardownAsync(window, closingManager);
                    return true; // true = cancel this close for now
                }

                _placement?.Save();
                _placement = null;
                _window = null;
                return false; // false = do not cancel, let it close
            });

            _window = window;
            _manager = manager;

            // index.html holds an <app> element, the same selector the main view uses
            _ = manager.Dispatcher.InvokeAsync(() =>
                manager.AddRootComponentAsync(typeof(ChartWindowRoot), "app", ParameterView.Empty));
            manager.Navigate("/");

            // Creates the native window. It does NOT block: the main window already started the
            // message loop, and Photino runs only one - see the note on TradingViewWindow.
            window.WaitForClose();

            // The handle and the list of monitors only exist once the window has been created
            WindowChrome.ApplyTitleBarTheme(window);
            _placement?.AfterCreate();
        }
        catch (Exception error)
        {
            _window = null;
            _manager = null;
            _placement = null;
            ScannerLog.Logger.Error(error, "ChartWindow");
            GlobalData.AddTextToLogTab("Could not open the chart window: " + error.Message);
        }
    }

    /// <summary>
    /// Take the chart page down while the browser is still there, then dispose the view and close
    /// the window. Disposing the view straight away (as until 07-11-2026) hung the whole application:
    /// PhotinoWebViewManager closes its message channel FIRST and only then disposes the page, and
    /// the page's Dispose still sends ChartWidget.dispose to the browser. SendMessage then retries
    /// the closed channel forever (Thread.Sleep(200) in a loop) on the UI thread - "not responding".
    /// With the root component removed beforehand nothing is left to send anything.
    /// </summary>
    private async Task CloseAfterTeardownAsync(PhotinoWindow window, PhotinoWebViewManager manager)
    {
        try
        {
            await manager.Dispatcher.InvokeAsync(() => manager.RemoveRootComponentAsync("app"));
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ChartWindow.RemoveRootComponent");
        }

        await DisposeManagerAsync(manager);

        try
        {
            _app.MainWindow.Invoke(() =>
            {
                try { window.Close(); }
                catch { /* already gone */ }
            });
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ChartWindow.CloseAfterTeardown");
        }
    }

    /// <summary>
    /// Dispose the view of a closed window: that disposes the chart page (which unsubscribes from
    /// the services) and its service scope, and stops the message pump of the view. Without it every
    /// opened and closed window would keep a chart alive that still listens to every update.
    /// </summary>
    private static async Task DisposeManagerAsync(PhotinoWebViewManager manager)
    {
        try
        {
            await manager.Dispatcher.InvokeAsync(async () => await manager.DisposeAsync());
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ChartWindow.DisposeManager");
        }
    }
}
