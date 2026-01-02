namespace CryptoScanner.Core.Services;

public interface IScannerSession
{
    void AfterStarup();
    void ApplySettings();
    void ConnectionWasLost(string text);
    void ConnectionWasRestored(string text);
    void ScheduleRefresh();
    void SetTimerDefaults();
    void Start(int delay);
    Task StopAsync();
}