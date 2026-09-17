using Avalonia.Controls;

namespace CryptoScanner.Emulator.Helpers;

/// <summary>
/// Puts a hover hint on the column headers of a DataGrid, so a percentage says what it divides by.
/// The Results grid has four of them (Win %, Profit %, Peak %, Return %) that look alike and mean
/// four different things; the hint carries the formula and the catch of each one.
/// <para>
/// A header declared as plain text in the axaml is replaced by a TextBlock with a ToolTip. The
/// column dialog and the layout store keep working: ColumnWindowViewModel.ExtractText reads the
/// text back out of a TextBlock, and the layout is keyed on the sort member path, not the header.
/// Columns whose header is not in the dictionary are left as they are.
/// </para>
/// </summary>
public static class ColumnHints
{
    public static void Apply(DataGrid grid, IReadOnlyDictionary<string, string> hints)
    {
        foreach (DataGridColumn column in grid.Columns)
        {
            if (column.Header is not string text || !hints.TryGetValue(text, out string? hint))
                continue;

            var header = new TextBlock { Text = text };
            ToolTip.SetTip(header, hint);
            column.Header = header;
        }
    }


    /// <summary>The Results grid, one hint per column, in the order of the grid.</summary>
    public static readonly IReadOnlyDictionary<string, string> Results = new Dictionary<string, string>
    {
        ["Id"] = "Number of the run in this session database.",
        ["Started"] = "When the run started (local time).",
        ["Label"] = "The label from the queue entry, plus the base interval and, when the entry had one, its own period.",
        ["Period"] = "The replayed window (from → to) and its length in days. Only runs over the same period can be compared.",
        ["Finished"] = "When the run finished (local time).",
        ["Duration"] = "How long the run itself took to compute (wall-clock), not how long positions lasted.",
        ["Result"] = "Outcome of the run: completed, stopped, or a recognised repeat of an earlier run (\"duplicate of run …\").",
        ["Signals"] = "Signals the strategies produced. More than one signal can lead to the same position, and a signal that fails the entry conditions leads to none.",
        ["Pos"] = "Positions the run opened: closed, still open and timed out together.",
        ["Open"] = "Positions still open when the run ended. Their result is NOT in Profit; see Best case and Worst case for what they can still do.",
        ["Won"] = "Closed positions with a positive result (after fees).",
        ["Lost"] = "Closed positions with a zero or negative result (after fees).",
        ["Timeout"] = "Positions whose entry order never filled and was cancelled. They never became a trade and are left out of Won, Lost and Win %.",
        ["Win %"] = "100 × Won / (Won + Lost). Timed out and still-open positions are left out. Says nothing about the size of the winners and losers.",
        ["Profit"] = "Realised profit of the CLOSED positions in the quote currency, fees included. Open positions are not in it.",
        ["Profit %"] = "100 × Profit / Invested. Invested is the summed stake of every trade — the same money going round — so this percentage shrinks with the number of trades and flatters a strategy that trades little. Use Peak % or Return % instead.",
        ["Invested"] = "Sum of the stake of every closed position, DCA parts included. NOT the capital the run needed: the same money goes round many times.",
        ["Peak cap."] = "Peak capital: the most that was tied up in open positions at any one moment. This is the money an account needed to run this.",
        ["Peak pos"] = "The largest number of positions open at the same time.",
        ["Peak %"] = "100 × Profit / Peak cap. — the return on the capital that was actually tied up. A stricter filter trades less and needs less capital, so compare the money as well.",
        ["Start cap."] = "The start capital of the run configuration. Only meaningful when asset management was on.",
        ["End cap."] = "Start cap. + Profit.",
        ["Return %"] = "100 × Profit / Start cap. A dash when asset management was off: then the start capital never limited what was traded and a percentage of it says nothing. Not per day — compared runs cover the same period.",
        ["Profit long"] = "Realised profit of the long positions only.",
        ["Profit short"] = "Realised profit of the short positions only.",
        ["Best case"] = "Profit + Open × average winner: what the run ends on if every still-open position closes as the average winner.",
        ["Worst case"] = "Profit + Open × average loser: what the run ends on if every still-open position closes as the average loser. The open positions lean to the losing side (the winners already hit take profit), so this is the one to look at.",
        ["Avg dur."] = "Average time from entry to close of the closed positions.",
        ["Min dur."] = "Shortest time from entry to close of the closed positions.",
        ["Max dur."] = "Longest time from entry to close of the closed positions.",
    };


    /// <summary>The Positions window of one run.</summary>
    public static readonly IReadOnlyDictionary<string, string> Positions = new Dictionary<string, string>
    {
        ["Id"] = "Number of the position in this session database.",
        ["Created"] = "When the position was opened (local time).",
        ["Closed"] = "When the position was closed (local time); a dash while it is still open.",
        ["Duration"] = "Time from entry to close.",
        ["Parts"] = "Filled parts: the entry counts as 1, every filled DCA part adds one. A trailing + means a DCA order is still pending on an open position.",
        ["Status"] = "Waiting = entry order placed, not filled yet. Trading = open. Ready = closed. Timeout = the entry order never filled. Cancelled = a newer signal invalidated the waiting position.",
        ["Profit"] = "Realised result in the quote currency, fees included. Meaningless while the position is still open (nothing has been returned yet).",
        ["Percentage"] = "Returned as a percentage of the invested stake, fees included: 100 = break-even, 101.50 = 1.5% profit, 98 = 2% loss. NOT the profit percentage itself — subtract 100.",
        ["Price"] = "The signal price the position was opened on.",
        ["SL %"] = "The stop-loss distance the strategy handed to the trader for this position, as a percentage of the entry. A dash means the global stop-loss percentage of the trading settings was used.",
        ["EventText"] = "The strategy's own text for the signal (the code, the setup, the intervals, the handed-over stop and take profit).",
    };


    /// <summary>The Signals window of one run.</summary>
    public static readonly IReadOnlyDictionary<string, string> Signals = new Dictionary<string, string>
    {
        ["Signal time"] = "Open time of the candle the signal fired on (local time).",
        ["Signal price"] = "The price the strategy reported for the signal, normally the close of that candle.",
        ["SL %"] = "The stop-loss distance the strategy handed over, as a percentage of the signal price. A dash means the global stop-loss percentage applies.",
        ["Valid"] = "ok = the signal passed the entry conditions; invalid = it was rejected (the reason is in the log).",
        ["Position"] = "The position this signal opened, or a dash when it opened none (rejected, a position was already open, or the entry timed out).",
        ["Pos. created"] = "When that position was opened (local time).",
    };
}
