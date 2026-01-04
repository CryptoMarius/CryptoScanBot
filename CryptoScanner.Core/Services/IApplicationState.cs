namespace CryptoScanner.Services;

public interface IApplicationState
{
    ApplicationOptions ApplicationOptions { get; set; }
    BarometerState BarometerState { get; set; }
    WindowState ChartWindow { get; set; }
    Dictionary<string, GridState> GridStates { get; set; }
    GridState LiveDataGrid { get; set; }
    WindowState MainWindow { get; set; }
    double MainWindowSplitterPosition { get; set; }
    GridState PositionClosedGrid { get; set; }
    GridState PositionOpenGrid { get; set; }
    GridState SignalGrid { get; set; }
    GridState SymbolGrid { get; set; }
    Dictionary<string, WindowState> WindowStates { get; set; }
}