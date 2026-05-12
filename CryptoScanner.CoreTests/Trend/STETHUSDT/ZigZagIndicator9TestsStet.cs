namespace CryptoScanner.CoreTests.Trend;

[TestClass()]
public class ZigZagIndicator9TestsStet : TestBase
{
    // STETHUSDT has a strange secondary trend (see two pictures).
    // The test was written with the old PivotList.Count loop and needs to be
    // rewritten on date-based checkpoints — see ZigZagIndicator9TestsEth for
    // the pattern to follow (DiscoverCheckpointValues + CalculateTest).

    //[TestMethod]
    //public void CalculateTestStetUsdt()
    //{
    //    // arrange
    //    InitTestSession();
    //    CryptoDatabase database = new();
    //    database.Open();

    //    CryptoSymbol symbol = CreateTestSymbol(database);
    //    CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
    //    CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

    //    string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Error assembly");
    //    LoadCandleDataFromDisk(symbolInterval.CandleList, Path.Combine(path, "Trend\\STETHUSDT\\STETHUSDT-1h.json"));

    //    // Trend via open/close
    //    ZigZagIndicator indicator = new(TrendType.Primary, false, 1.0m);

    //    // Checkpoints (old PivotList.Count format — needs rewriting):
    //    // I=679 D=2024-12-09 12:00  "a L and the box is broken"
    //    // I=680 D=2024-12-09 15:00  "a created high"
    //    // I=681 D=2024-12-09 16:00  "a L box broken, new high created"
    //    // I=682 D=2024-12-10 01:00  "a H but this one is lower than the last L"
    //    // I=683 D=2024-12-10 03:00  "a L and the box is broken"
    //}
}
