using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal;

// Een oude enumeratie "SignalStrategy" is vervallen en overgenomen door de combinatie
//  van Mode en SignalStrategy (zonder de oversold/overbought toevoegingen)
public class AlgorithmDefinition
{
    public required string Name { get; set; }
    public required CryptoSignalStrategy Strategy { get; set; }
    public required Type? AnalyzeLongType { get; set; }
    public required Type? AnalyzeShortType { get; set; }

    public SignalCreateBase? InstantiateAnalyzeLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) 
        => (SignalCreateBase?)Activator.CreateInstance(AnalyzeLongType, [symbol, interval, candle]);

    public SignalCreateBase? InstantiateAnalyzeShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) 
        => (SignalCreateBase?)Activator.CreateInstance(AnalyzeShortType, [symbol, interval, candle]);
}
