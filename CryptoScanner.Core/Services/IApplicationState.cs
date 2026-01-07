namespace CryptoScanner.Core.Services;

public interface IApplicationState
{
    //ApplicationOptions ApplicationOptions { get; set; }
    BarometerState BarometerState { get; set; }
    double MainWindowSplitterPosition { get; set; }
    Dictionary<string, GridState> GridStates { get; set; }
    Dictionary<string, WindowState> WindowStates { get; set; }
}