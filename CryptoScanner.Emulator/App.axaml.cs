using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.Views;

namespace CryptoScanner.Emulator;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }


    /// <summary>
    /// Applies the scanner's dark/light/system theme choice (<c>Settings.General.Theme</c>) to the
    /// emulator. The live scanner does this in ScannerSession.Start; the emulator doesn't run that
    /// session, so without this call it would always stay on the default (system) variant and
    /// ignore the user's settings.json choice. Call after settings are loaded and again after the
    /// Configure dialog so a changed theme takes effect immediately.
    /// </summary>
    public static void ApplyThemeFromSettings()
    {
        if (Current == null)
            return;

        ThemeVariant chosen = GlobalData.Settings.General.Theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        if (Current.RequestedThemeVariant != chosen)
            Current.RequestedThemeVariant = chosen;
    }


    public override void OnFrameworkInitializationCompleted()
    {
        // Register the DI container before any window is built. SecureStringConverter (used
        // by SaveConfiguration when settings.json contains credentials) reaches into
        // GlobalData.Services on its very first construction — without a registered
        // IStringProtectorService its ctor throws TargetInvocationException deep inside the
        // System.Text.Json type-info pipeline.
        GlobalData.Services = EmulatorServices.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Two-stage startup:
            // 1. SetupWindow asks for data folder + exchange (DB doesn't exist yet, so the
            //    exchange combo is fed from the static seed list in CryptoDatabase).
            // 2. On OK: apply the folder, optionally seed settings.json from the live scanner,
            //    run EmulatorBootstrap (DB + LoadExchanges/Symbols/Settings + strategy index),
            //    then swap in the MainWindow.
            //    On Cancel: shut down the application.
            var setup = new SetupWindow();
            setup.Closed += async (_, _) => await OnSetupClosed(desktop, setup);
            desktop.MainWindow = setup;
        }
        base.OnFrameworkInitializationCompleted();
    }


    private static async Task OnSetupClosed(IClassicDesktopStyleApplicationLifetime desktop, SetupWindow setup)
    {
        if (!setup.ViewModel.Confirmed)
        {
            desktop.Shutdown();
            return;
        }

        // 1. Apply the chosen data folder. Everything that reads GlobalData.AppDataFolder
        //    after this point — DB connection, settings.json, candle DBs — points here.
        GlobalData.AppDataFolder = setup.ViewModel.DataFolder;
        Directory.CreateDirectory(GlobalData.AppDataFolder);

        // 2. Logging must be initialised AFTER AppDataFolder is known (the NLog files land in that
        //    folder's Log subdirectory) but BEFORE Bootstrap runs so its first AddTextToLogTab
        //    calls already write to file and the in-app Log tab. Same NLog setup as the scanner:
        //    a default log, an error log, and (in DEBUG) a Trace log. EmulatorLogBridge then forwards
        //    every AddTextToLogTab line to ScannerLog.Logger.Info — independently of the UI Log tab —
        //    so those lines (bootstrap, runs, the Timing line, the per-run log) always land on disk.
        ScannerLog.InitializeLogging();
        EmulatorLogBridge.Start();

        // 3. Seed settings.json from the live scanner if this is a fresh emulator folder.
        //    Keeps the same convention as before; only the call site moved out of Program.cs.
        BootstrapFromLiveScanner();

        // 4. Initialise the data plumbing — DB, exchanges, intervals, settings, strategies,
        //    plus the per-exchange candle loads so LastCandleSynchronized is primed before
        //    the user clicks Fetch candles. The setup-chosen exchange wins.
        await EmulatorBootstrap.InitializeAsync(setup.ViewModel.SelectedExchange);

        // 4b. Apply the dark/light/system theme from settings.json now that settings are loaded.
        //     The live scanner does this in ScannerSession.Start, which the emulator never runs.
        ApplyThemeFromSettings();

        // 5. Now show the actual main window.
        var main = new MainWindow();
        desktop.MainWindow = main;
        main.Show();

        // Signal readiness. Logged AFTER the MainWindow exists so the LogTabViewModel (which
        // subscribes in its constructor) is already hooked and shows it; the file log captures
        // it too. Tells the user bootstrap finished and the app is ready for input.
        GlobalData.AddTextToLogTab($"Emulator ready — exchange {GlobalData.ActiveExchange?.Name}, data folder {GlobalData.AppDataFolder}");
    }


    /// <summary>
    /// One-time copy of the live scanner's CryptoScanBot-settings.json into the emulator
    /// folder when the emulator has no settings file yet. The live folder is the parent of
    /// the emulator folder by convention (..\CryptoScanBot\Emulator → ..\CryptoScanBot).
    /// No-op if the user picked a folder that doesn't fit the convention or the live folder
    /// has no settings — the emulator then starts fresh and the user fills in the dialog.
    /// </summary>
    private static void BootstrapFromLiveScanner()
    {
        string filename = $"{Constants.AppName}-settings.json";
        string emulatorSettings = Path.Combine(GlobalData.AppDataFolder, filename);
        if (File.Exists(emulatorSettings))
            return;

        string? liveFolder = Path.GetDirectoryName(GlobalData.AppDataFolder);
        if (string.IsNullOrEmpty(liveFolder))
            return;

        string liveSettings = Path.Combine(liveFolder, filename);
        if (!File.Exists(liveSettings))
            return;

        try
        {
            File.Copy(liveSettings, emulatorSettings);
        }
        catch
        {
            // Non-fatal: emulator simply starts with default settings.
        }
    }
}
