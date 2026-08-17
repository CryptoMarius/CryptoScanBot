namespace CryptoScanner.Core.Services;

public interface IScannerSession
{
    void AfterStartup();
    Task ApplyConfigurationAsync(bool loadSymbols);
    void ConnectionWasLost(string text);
    void ConnectionWasRestored(string text, TimeSpan downtime);
    void ScheduleRefresh();
    void SetTimerDefaults();
    void Start(int delay);
    Task StopAsync();
}