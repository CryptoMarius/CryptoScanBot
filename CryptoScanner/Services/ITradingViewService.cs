using CryptoScanner.ViewModels;

namespace CryptoScanner.Services;

public interface ITradingViewService
{
    IEnumerable<DashboardSymbolViewModel> TvSymbols { get; set; }

    void Start();
    void Stop();
}
