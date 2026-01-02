namespace CryptoScanner.Services;

public interface IApplicationState
{
    WindowState MainWindow { get; set; }
    double MainWindowSplitterPosition { get; set; }
    GridState SignalGrid { get; set; }
    GridState SymbolGrid { get; set; }
}