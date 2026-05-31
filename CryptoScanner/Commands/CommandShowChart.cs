using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;

namespace CryptoScanner.Commands;

public class CommandShowChart : CommandBase
{
    private const string defaultInterval = "15m";
    private static ChartWindow? ChartWindow = null;

    public override void Execute(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandShowGraph");
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.parentWindow != null)
        {
            try
            {
                // Implement your external program logic here
                System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in internal program");

                if (ChartWindow == null || !ChartWindow.IsVisible)
                {
                    ChartWindow = new ChartWindow
                    {
                        //DataContext = vm1,
                        CanResize = true,
                        Title = "Chart form",
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    };

                    if (ChartWindow.DataContext is ChartWindowViewModel vm2)
                    {
                        vm2.SymbolSelector.SelectedBase = dto.symbol.Base;
                        vm2.SymbolSelector.SelectedQuote = dto.symbol.Quote;
                        if (dto.interval != null)
                            vm2.SymbolSelector.SelectedInterval = dto.interval.Name;
                        else
                            vm2.SymbolSelector.SelectedInterval = defaultInterval;
                    }
                    else throw new Exception("Problem chart viewmodel");

                    ChartWindow.Show();
                }
                else
                {
                    if (ChartWindow.DataContext is ChartWindowViewModel vm1)
                    {
                        vm1.HideAnnototionCursor();
                        vm1.SymbolSelector.SelectedBase = dto.symbol.Base;
                        vm1.SymbolSelector.SelectedQuote = dto.symbol.Quote;
                        if (dto.interval != null)
                            vm1.SymbolSelector.SelectedInterval = dto.interval.Name;
                        else
                            vm1.SymbolSelector.SelectedInterval = defaultInterval;
                    }

                    // Restore if minimized
                    if (ChartWindow.WindowState == WindowState.Minimized)
                        ChartWindow.WindowState = WindowState.Normal;

                    // Bring window to the front
                    ChartWindow.Activate();
                }

            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error showing chart {dto.symbol.Name} {dto.interval?.Name} {error.Message}");
            }
        }
    }
}
