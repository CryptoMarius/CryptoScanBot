using CryptoScanner.Core.Core;
using CryptoScanner.Core.Excel;
using CryptoScanner.Emulator.ViewModels;

using NPOI.SS.UserModel;

namespace CryptoScanner.Emulator.Services;

/// <summary>
/// The runs of the Results grid as an Excel sheet, one row per run, with the figures as numbers so
/// they can be sorted and calculated on. Only what the grid shows: the stored settings, the run
/// configuration and the other JSON blobs stay out. Written to the Excel folder of the data folder,
/// like the other exports (ExcelBase.StartExcell).
/// </summary>
public class ExcelRunsDump : ExcelBase
{
    private readonly ICellStyle CellStyleAmount;
    private readonly ICellStyle CellStyleDay;
    private readonly ICellStyle CellStyleDuration;

    public ExcelRunsDump() : base("Emulator runs")
    {
        IDataFormat format = Book.CreateDataFormat();

        CellStyleAmount = Book.CreateCellStyle();
        CellStyleAmount.DataFormat = format.GetFormat("#,##0.00");

        CellStyleDay = Book.CreateCellStyle();
        CellStyleDay.DataFormat = format.GetFormat("dd-MM-yyyy");
        CellStyleDay.Alignment = HorizontalAlignment.Left;

        // A duration as a fraction of days, shown in hours so a run of more than a day stays readable
        CellStyleDuration = Book.CreateCellStyle();
        CellStyleDuration.DataFormat = format.GetFormat("[h]:mm:ss");
    }


    private static DateTime? Local(DateTime? utc)
        => utc.HasValue ? DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToLocalTime() : null;

    private static double? Days(double? seconds) => seconds / 86400.0;


    protected void DumpRuns(IReadOnlyList<RunRow> rows)
    {
        ISheet sheet = Book.CreateSheet("Runs");
        int row = 0;

        string[] headers =
        [
            "Id", "Label", "From", "To", "Days", "Started", "Finished", "Duration", "Build", "Result",
            "Signals", "Positions", "Open", "Won", "Lost", "Timeout", "Win %",
            "Profit", "Unrealized PnL", "Total PnL", "Total PnL Long", "Total PnL Short", "Profit %",
            "Invested", "Peak cap.", "Peak pos", "Peak %", "Drawdown %", "Drawdown USDT",
            "Start cap.", "End cap.", "Return %", "Recovery factor", "Counts", "Without best 10",
            "Exposure %", "RAR %", "Capital use estimated",
            "Long", "Profit long", "Short", "Profit short", "Average win", "Average loss", "Best case", "Worst case",
            "Avg dur.", "Min dur.", "Max dur.",
        ];
        for (int i = 0; i < headers.Length; i++)
            WriteCell(sheet, i, row, headers[i]);

        foreach (RunRow r in rows)
        {
            ++row;
            int column = 0;

            WriteCell(sheet, column++, row, r.Id);
            WriteCell(sheet, column++, row, r.Label);
            WriteCell(sheet, column++, row, r.FromDate, CellStyleDay);
            WriteCell(sheet, column++, row, r.ToDate, CellStyleDay);
            WriteCell(sheet, column++, row, r.FromDate.HasValue && r.ToDate.HasValue
                ? (int)Math.Round((r.ToDate.Value - r.FromDate.Value).TotalDays) : null);
            WriteCell(sheet, column++, row, Local(r.StartedAt), CellStyleDate);
            WriteCell(sheet, column++, row, Local(r.FinishedAt), CellStyleDate);
            WriteCell(sheet, column++, row, ((r.FinishedAt ?? DateTime.UtcNow) - r.StartedAt).TotalDays, CellStyleDuration);
            WriteCell(sheet, column++, row, Local(r.BuildStamp), CellStyleDate);
            WriteCell(sheet, column++, row, r.Result);

            WriteCell(sheet, column++, row, r.SignalCount);
            WriteCell(sheet, column++, row, r.PositionCount);
            WriteCell(sheet, column++, row, r.PositionsOpen);
            WriteCell(sheet, column++, row, r.PositionsWon);
            WriteCell(sheet, column++, row, r.PositionsLost);
            WriteCell(sheet, column++, row, r.PositionsTimeout);
            WriteCell(sheet, column++, row, r.WinPercentage, CellStyleAmount);

            WriteCell(sheet, column++, row, r.Profit, CellStyleAmount);
            WriteCell(sheet, column++, row, r.UnrealizedPnl, CellStyleAmount);
            WriteCell(sheet, column++, row, r.TotalPnl, CellStyleAmount);
            WriteCell(sheet, column++, row, r.TotalPnlLong, CellStyleAmount);
            WriteCell(sheet, column++, row, r.TotalPnlShort, CellStyleAmount);
            WriteCell(sheet, column++, row, r.ProfitPercentage, CellStyleAmount);

            WriteCell(sheet, column++, row, r.Invested, CellStyleAmount);
            WriteCell(sheet, column++, row, r.PeakInvested, CellStyleAmount);
            WriteCell(sheet, column++, row, r.PeakPositions);
            WriteCell(sheet, column++, row, r.PeakProfitPercentage, CellStyleAmount);
            WriteCell(sheet, column++, row, r.MaxDrawdownPercentage, CellStyleAmount);
            WriteCell(sheet, column++, row, r.OpenDrawdown, CellStyleAmount);

            // Same rule as the grid: start/end capital and return only mean something with asset management on
            WriteCell(sheet, column++, row, r.TotalStartCapital, CellStyleAmount);
            WriteCell(sheet, column++, row, r.HasCapitalReturn ? r.EndCapital : null, CellStyleAmount);
            WriteCell(sheet, column++, row, r.HasCapitalReturn ? r.CapitalReturnPercentage : null, CellStyleAmount);
            WriteCell(sheet, column++, row, r.RecoveryFactor, CellStyleAmount);
            // The greyed-out mark of the grid as a column of its own
            WriteCell(sheet, column++, row, r.RecoveryFactorCounts ? "yes" : "no");
            WriteCell(sheet, column++, row, r.ProfitWithoutBestTen, CellStyleAmount);

            WriteCell(sheet, column++, row, r.ExposurePercentage, CellStyleAmount);
            WriteCell(sheet, column++, row, r.RiskAdjustedReturn, CellStyleAmount);
            WriteCell(sheet, column++, row, r.CapitalUseEstimated == true ? "yes" : "");

            WriteCell(sheet, column++, row, r.PositionsLong);
            WriteCell(sheet, column++, row, r.ProfitLong, CellStyleAmount);
            WriteCell(sheet, column++, row, r.PositionsShort);
            WriteCell(sheet, column++, row, r.ProfitShort, CellStyleAmount);
            WriteCell(sheet, column++, row, r.AverageWin, CellStyleAmount);
            WriteCell(sheet, column++, row, r.AverageLoss, CellStyleAmount);
            WriteCell(sheet, column++, row, r.BestCase, CellStyleAmount);
            WriteCell(sheet, column++, row, r.WorstCase, CellStyleAmount);

            WriteCell(sheet, column++, row, Days(r.AvgDurationSec), CellStyleDuration);
            WriteCell(sheet, column++, row, Days(r.MinDurationSec), CellStyleDuration);
            WriteCell(sheet, column++, row, Days(r.MaxDurationSec), CellStyleDuration);
        }

        AutoSize(sheet, headers.Length);
        // AutoSize makes the label column as wide as the longest label; cap it so the figures stay in view
        sheet.SetColumnWidth(1, Math.Min(sheet.GetColumnWidth(1), 100 * 256));
        sheet.CreateFreezePane(2, 1);
        sheet.SetAutoFilter(new NPOI.SS.Util.CellRangeAddress(0, row, 0, headers.Length - 1));
    }


    /// <summary>
    /// Writes the runs to "&lt;data folder&gt;\Excel\&lt;folder name&gt; runs.xls" and opens it.
    /// Returns the path; throws when the file cannot be written (most often: still open in Excel).
    /// </summary>
    public string ExportToExcel(IReadOnlyList<RunRow> rows)
    {
        GlobalData.AddTextToLogTab($"Dumping {rows.Count} run(s) to Excel");
        DumpRuns(rows);

        // The folder name (Session2, Emulator.Hyperliquid, ...) keeps the files of two data folders apart
        string folderName = Path.GetFileName(GlobalData.AppDataFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return StartExcell("runs", folderName);
    }
}
