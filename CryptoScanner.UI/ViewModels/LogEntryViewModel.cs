using CryptoScanner.Core.Enums;

namespace CryptoScanner.UI.ViewModels;

public class LogEntryViewModel
{
    public DateTime Date { get; init; }
    public string Text { get; init; } = "";

    public string GetCellValue(LogColumnEnum column)
    {
        return column switch
        {
            LogColumnEnum.Date => Date.ToString("yyyy-MM-dd HH:mm:ss"),
            LogColumnEnum.Text => Text,
            _ => "",
        };
    }
}
