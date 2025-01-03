﻿namespace CryptoScanBot.Commands;

public enum Command
{
    None,
    ActivateTradingApp,
    ActivateActiveExchange,
    ActivateTradingviewIntern,
    ActivateTradingviewExtern,
    ShowTrendInformation,
    ExcelSignalInformation,
    ExcelSignalsInformation,
    ExcelSymbolInformation,
    ExcelExchangeInformation,
    ExcelPositionInformation,
    ExcelPositionsInformation,
    CopyDataGridCells,
    CopySymbolInformation,
    ScannerSessionDebug,
    PositionCalculate,
    TradingViewImportList,
    ShowSymbolGraph,
    About,
    CalculateAllLiquidityZones,
    CalculateSymbolLiquidityZones,
}

// Work in progres, opzetje tichting ICommand (teveel werk op dit moment)
// De parameters zijn/worden aardig complex) -> eens nazoeken hoe en wat)

public abstract class CommandBase
{
    public abstract void Execute(ToolStripMenuItemCommand item, object sender);
}
