using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.ViewModels;
using CryptoScanner.Visualisation.ViewModels;
using CryptoScanner.Visualisation.Views;

namespace CryptoScanner.Commands;

public class CommandShowGraph : CommandBase
{
    private static VisualisationWindow? VisualisationWindow = null;

    public override void Execute(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandShowGraph");
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.interval != null && dto.parentWindow != null)
        {
            // Implement your external program logic here
            System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in internal program");


            var vm = new VisualisationViewModel();
            vm.SymbolSelector.SelectedBase = dto.symbol.Base;
            vm.SymbolSelector.SelectedQuote = dto.symbol.Quote;
            if (dto.interval != null)
                vm.SymbolSelector.SelectedInterval = dto.interval.Name;
            else
                vm.SymbolSelector.SelectedInterval = GlobalData.IntervalListPeriod[Core.Enums.CryptoIntervalPeriod.interval5m].Name;


            if (VisualisationWindow == null)
            {
                VisualisationWindow = new VisualisationWindow
                {
                    DataContext = vm,
                    CanResize = true,
                    Title = "Chart form",
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                VisualisationWindow.Show(dto.parentWindow);
            }
            else
            {
                VisualisationWindow.DataContext = vm;
                VisualisationWindow.BringIntoView();
            }
        }
    }
}
