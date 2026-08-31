using CryptoScanner.Core.Core;
using CryptoScanner.Core.Sounds;

namespace CryptoScanner.Core.Services;

/// <summary>
/// Handles the suspend and resume notifications of the operating system, shared by every user
/// interface (Avalonia, Photino and Web).
///
/// Suspend and resume used to be handled fire-and-forget, so they could overlap. Windows freezes the
/// process a couple of seconds after the suspend event, while the teardown takes far longer than that
/// (saving the candles of a few hundred symbols is about a minute), so the suspend was always still
/// running when the resume arrived. The resume then found a session that was still marked as started,
/// silently did nothing, reported "Reconnected successfully" anyway, and the teardown that finished
/// afterwards left the scanner dead until the application was restarted. The events are therefore
/// handled one at a time.
/// </summary>
public static class PowerModeHandler
{
    private static readonly SemaphoreSlim PowerModeLock = new(1, 1);

    /// <summary>
    /// How long an event waits for a previous event that is still running. Comfortably longer than a
    /// candle save of a few hundred symbols, so under normal circumstances the wait always succeeds.
    /// </summary>
    private static readonly TimeSpan WaitForPreviousEvent = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Number of attempts the resume makes to get the scanner session running again.
    /// </summary>
    private const int ResumeAttempts = 3;
    private static readonly TimeSpan ResumeRetryDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Delay before reconnecting, to give the network adapters time to come back up.
    /// </summary>
    private static readonly TimeSpan ResumeNetworkDelay = TimeSpan.FromSeconds(2);


    /// <summary>
    /// Event handler to attach to <see cref="PowerMonitorService.PowerModeChanged"/>.
    /// </summary>
    public static void Handle(object? sender, PowerModeEventArgs e) => _ = HandleAsync(e.Mode);


    public static async Task HandleAsync(PowerMode mode)
    {
        if (mode != PowerMode.Suspend && mode != PowerMode.Resume)
            return;

        bool acquired = false;
        try
        {
            acquired = await PowerModeLock.WaitAsync(WaitForPreviousEvent).ConfigureAwait(false);
            if (!acquired)
            {
                // Continue anyway: refusing the resume would leave the scanner down for certain, while
                // the retry below still has a chance of picking it up once the suspend releases it.
                ScannerLog.Logger.Trace($"Power mode {mode}: previous power event still running after {WaitForPreviousEvent.TotalMinutes:N0} minutes");
                GlobalData.AddTextToLogTab($"Power mode {mode}: the previous power event is still running, continuing anyway");
            }

            switch (mode)
            {
                case PowerMode.Suspend:
                    await SuspendAsync().ConfigureAwait(false);
                    break;

                case PowerMode.Resume:
                    await ResumeAsync().ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, $"Error handling power mode change: {mode}");
            GlobalData.AddTextToLogTab($"Power mode {mode} error: {ex.Message}");
        }
        finally
        {
            if (acquired)
                PowerModeLock.Release();
        }
    }


    private static async Task SuspendAsync()
    {
        ScannerLog.Logger.Trace("System going to sleep - disconnecting...");
        GlobalData.AddTextToLogTab("System going to sleep - disconnecting...");

        if (GlobalData.SignalRService != null)
            await GlobalData.SignalRService.StopAsync().ConfigureAwait(false);

        var scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("ScannerSession not registered");
        await scannerSession.StopAsync().ConfigureAwait(false);

        ThreadSoundPlayer.StopSoundThread();
        //await DataStore.SaveCandlesAsync(); included in scannerSession.StopAsync()
        GlobalData.AddTextToLogTab("Disconnected successfully");
    }


    private static async Task ResumeAsync()
    {
        ScannerLog.Logger.Trace("System resumed - reconnecting...");
        GlobalData.AddTextToLogTab("System resumed - reconnecting...");

        await Task.Delay(ResumeNetworkDelay).ConfigureAwait(false); // wait for network

        if (GlobalData.SignalRService != null)
            await GlobalData.SignalRService.StartAsync().ConfigureAwait(false);

        var scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("ScannerSession not registered");

        // Retry, for the case where the suspend was still tearing the session down. A session that
        // refuses to start is invisible to the user until the scanner has been silent for hours.
        for (int attempt = 1; attempt <= ResumeAttempts; attempt++)
        {
            // Only the first attempt keeps the hibernate delay; by the later attempts the network has
            // had more than enough time to come back.
            if (scannerSession.Start(attempt == 1 ? 5000 : 0))
            {
                GlobalData.AddTextToLogTab("Reconnected successfully");
                return;
            }

            if (attempt < ResumeAttempts)
            {
                GlobalData.AddTextToLogTab($"Reconnect attempt {attempt} of {ResumeAttempts} could not start the scanner, retrying in {ResumeRetryDelay.TotalSeconds:N0} seconds");
                await Task.Delay(ResumeRetryDelay).ConfigureAwait(false);
            }
        }

        ScannerLog.Logger.Error($"Resume failed: the scanner session did not start after {ResumeAttempts} attempts");
        GlobalData.AddTextToLogTab($"Reconnect failed: the scanner did not start after {ResumeAttempts} attempts, please restart the application");
    }
}
