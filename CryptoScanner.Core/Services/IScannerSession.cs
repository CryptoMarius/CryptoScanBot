namespace CryptoScanner.Core.Services;

public interface IScannerSession
{
    void AfterStartup();
    Task ApplyConfigurationAsync(bool loadSymbols);
    void ConnectionWasLost(string text);
    void ConnectionWasRestored(string text, TimeSpan downtime);
    void ScheduleRefresh();
    void SetTimerDefaults();
    /// <summary>
    /// Start the session. Returns whether the session is running afterwards: false when a stop is
    /// still in progress, because that stop would tear down whatever is started here.
    /// </summary>
    bool Start(int delay);
    Task StopAsync();
}