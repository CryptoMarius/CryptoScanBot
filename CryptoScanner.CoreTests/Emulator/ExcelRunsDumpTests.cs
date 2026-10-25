using CryptoScanner.Emulator.Services;
using CryptoScanner.Emulator.ViewModels;

using NPOI.SS.UserModel;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The Excel export of the Results grid: one row per run under a header row, the figures as numbers,
/// and nothing of the stored JSON. Filled through a subclass, so no file is written and Excel is not opened.
/// </summary>
[TestClass]
public class ExcelRunsDumpTests
{
    private class Probe : ExcelRunsDump
    {
        public ISheet Fill(IReadOnlyList<RunRow> rows)
        {
            DumpRuns(rows);
            return Book.GetSheet("Runs");
        }
    }

    private static int Column(ISheet sheet, string header)
    {
        IRow head = sheet.GetRow(0);
        for (int i = 0; i < head.LastCellNum; i++)
            if (head.GetCell(i)?.StringCellValue == header)
                return i;
        Assert.Fail($"No column \"{header}\"");
        return -1;
    }

    [TestMethod]
    public void OneRowPerRun_FiguresAsNumbers()
    {
        RunRow run = new()
        {
            Id = 634, Label = "N01 storsi.multi", StartedAt = new DateTime(2026, 10, 24, 20, 37, 0),
            FinishedAt = new DateTime(2026, 10, 24, 21, 37, 0),
            FromDate = new DateTime(2026, 1, 1), ToDate = new DateTime(2026, 10, 1),
            PositionCount = 3410, PositionsWon = 3376, PositionsLost = 17, Profit = 2614.25m,
            EquityStart = 100000m, EquityEnd = 102500m, StartCapital = 100000m, UseAssetManagement = true,
            AvgDurationSec = 86400 * 1.5,
        };
        ISheet sheet = new Probe().Fill([run, new RunRow { Id = 635 }]);

        Assert.AreEqual(2, sheet.LastRowNum);
        IRow row = sheet.GetRow(1);
        Assert.AreEqual(634, row.GetCell(Column(sheet, "Id")).NumericCellValue);
        Assert.AreEqual("N01 storsi.multi", row.GetCell(Column(sheet, "Label")).StringCellValue);
        Assert.AreEqual(273, row.GetCell(Column(sheet, "Days")).NumericCellValue);
        Assert.AreEqual(2614.25, row.GetCell(Column(sheet, "Profit")).NumericCellValue, 1e-9);
        Assert.AreEqual(-114.25, row.GetCell(Column(sheet, "Unrealized PnL")).NumericCellValue, 1e-9);
        Assert.AreEqual(2.61425, row.GetCell(Column(sheet, "Return %")).NumericCellValue, 1e-9);
        Assert.AreEqual(1.5, row.GetCell(Column(sheet, "Avg dur.")).NumericCellValue, 1e-9);
        Assert.AreEqual(new DateTime(2026, 1, 1), row.GetCell(Column(sheet, "From")).DateCellValue);

        // A run without an equity curve leaves those cells empty instead of writing a zero
        Assert.AreEqual(CellType.Blank, sheet.GetRow(2).GetCell(Column(sheet, "Total PnL")).CellType);
    }

    [TestMethod]
    public void NoJsonColumns()
    {
        ISheet sheet = new Probe().Fill([new RunRow { Id = 1 }]);
        IRow head = sheet.GetRow(0);
        for (int i = 0; i < head.LastCellNum; i++)
            Assert.IsFalse(head.GetCell(i).StringCellValue.Contains("json", StringComparison.OrdinalIgnoreCase));
    }
}
