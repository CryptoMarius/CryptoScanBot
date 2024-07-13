namespace CryptoScanBot.Commands;

public enum Command
{
    None,
    ActivateTradingApp,
    ActivateActiveExchange,
    ActivateTradingviewIntern,
    ActivateTradingviewExtern,
    ShowTrendInformation,
    ExcelSignalInformation,
    ExcelSymbolInformation,
    ExcelExchangeInformation,
    ExcelPositionInformation,
    //CopySignalInformation,
    CopySymbolInformation,
    ScannerSessionDebug,
    PositionCalculate,
    About
}

// Work in progres, opzetje tichting ICommand (teveel werk op dit moment)
// De parameters zijn/worden aardig complex) -> eens nazoeken hoe en wat)

public abstract class CommandBase
{
    public abstract void Execute(object sender);
    public abstract string CommandName();
}
