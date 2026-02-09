namespace CryptoScanner.Core.Model;

// Just a dummy class for UI inheritance, not used for anything else. The actual log entries are stored in LogViewModel.

public partial class CryptoLog
{
    public DateTime Date { get; set; }
    public string Text { get; set; } = string.Empty;
}
