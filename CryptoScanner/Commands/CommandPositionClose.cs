using Avalonia.Controls;
using Avalonia.Threading;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Trader;
using CryptoScanner.Views;

namespace CryptoScanner.Commands;

/// <summary>
/// "Position close": take the profit or the loss of the selected position right now. The work
/// itself is in <see cref="ManualExit"/>, shared with the Blazor grid; this command only adds the
/// confirmation and the row refresh.
/// </summary>
public class CommandPositionClose : CommandBase
{
    public override bool CanExecute(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.position != null)
            return ManualExit.CanExit(dto.position);
        return false;
    }

    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandPositionClose");
        if (!GetObjectInformation(parameter, out ParameterObjects dto) || dto.symbol == null || dto.position == null || dto.parentWindow == null)
            return;

        // Money moves on this one, so ask first (the delete-all does the same)
        var dialog = new ConfirmDialog(ManualExit.ConfirmationText(dto.position), "Close position")
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var confirmed = await dialog.ShowDialog<bool?>(dto.parentWindow);
        if (confirmed != true)
            return;

        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();
            await ManualExit.RequestExitAsync(databaseThread, dto.position);

            // Refresh() raises INotifyPropertyChanged, which Avalonia's bindings must receive on the UI thread.
            if (dto.PositionViewModel != null)
                await Dispatcher.UIThread.InvokeAsync(dto.PositionViewModel.Refresh);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"error closing position {dto.position.Id} {dto.symbol.Name} {error.Message}");
        }
    }
}
