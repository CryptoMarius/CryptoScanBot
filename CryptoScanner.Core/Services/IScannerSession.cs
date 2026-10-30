namespace CryptoScanner.Core.Services;

public interface IScannerSession
{
    void AfterStartup();
    /// <summary>
    /// The exchange the user picked on a new installation (GlobalData.ExchangeChoicePending). Sets
    /// both the scanned exchange and the exchange the trading app opens on, and saves the settings.
    /// Call it before ApplyConfigurationAsync and Start.
    /// </summary>
    void ChooseExchange(string exchangeName);
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