namespace CryptoScanner.Core.Services;

/// <summary>
/// The keys under which the grid state (sort order, column width, visibility and display order) is
/// stored in ApplicationState.GridStates.
///
/// Both hosts write into the same CryptoScanBot-user.json, so a name that differs between them means
/// the two applications each keep their own half of the settings and neither sees what the other
/// saved. They used to: Avalonia wrote "SignalGrid" while Photino wrote "Signal", which left twelve
/// blocks in the file instead of six. Hence one list here, used by both hosts, instead of a string
/// literal per view.
/// </summary>
public static class GridNames
{
    public const string Symbol = "SymbolGrid";
    public const string Signal = "SignalGrid";
    public const string LiveData = "LiveDataGrid";
    public const string PositionOpen = "PositionOpenGrid";
    public const string PositionClosed = "PositionClosedGrid";
    public const string Log = "LogGrid";
}
