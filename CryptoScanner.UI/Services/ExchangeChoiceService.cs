using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

namespace CryptoScanner.UI.Services;

/// <summary>
/// Holds back the start of the scanner on a new installation until an exchange has been chosen. The
/// host hands over the start with <see cref="Request"/> instead of running it, ExchangeChoiceDialog in
/// the main layout asks the question and calls <see cref="Choose"/>. The Blazor counterpart of the
/// Avalonia ExchangeChoiceWindow.
/// </summary>
public class ExchangeChoiceService
{
    private Action? _startScanner;

    /// <summary>True while the question is open and the scanner has not been started.</summary>
    public bool IsPending => Volatile.Read(ref _startScanner) != null;

    /// <summary>
    /// Raised when the question opens or has been answered. Every open front end (one per browser
    /// tab on the web host) shows the dialog, and the first answer closes all of them.
    /// </summary>
    public event Action? Changed;

    /// <summary>Called by the host instead of starting the scanner itself.</summary>
    public void Request(Action startScanner)
    {
        Volatile.Write(ref _startScanner, startScanner);
        Changed?.Invoke();
    }

    public void Choose(string exchangeName)
    {
        // Two front ends answering at the same time must not start the scanner twice
        Action? startScanner = Interlocked.Exchange(ref _startScanner, null);
        if (startScanner == null)
            return;

        var scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("ScannerSession not registered");
        scannerSession.ChooseExchange(exchangeName);
        startScanner();

        Changed?.Invoke();
    }
}
