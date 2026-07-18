using Avalonia.Controls;

using CryptoScanner.Analyzers.Storsi;
using CryptoScanner.Config.ViewModels;
using CryptoScanner.Config.Views;
using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.StoRsi.Config;

public class StorsiConfigView : IConfigView
{
    private readonly StrategyBreTabViewModel _viewModel = new();

    public string TabHeader => "StoRsi";

    public Control CreateSettingsView()
    {
        return new StrategyBreTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        var s = StoRsiPlugin.Settings;

        _viewModel.SoundAndColorsViewModel.LoadConfig("Bre", s);
        _viewModel.StrategyEntryConditionsViewModel.LoadConfig(s);

        var vm = _viewModel.StrategyBreSettingsViewModel; // todo storsi!
        vm.BandLength = s.BandLength;
        vm.OuterMult = s.OuterMult;
        vm.DidoLength = s.DidoLength;
        vm.DidoMult = s.DidoMult;
        vm.UseTrendFilter = s.UseTrendFilter;
        vm.HmaLength = s.HmaLength;
        vm.UseRsiFilter = s.UseRsiFilter;
        vm.RequireStochOsOb = s.RequireStochOsOb;
        vm.AllowStack = s.AllowStack;
        vm.UseStopLoss = s.UseStopLoss;
        vm.TimeframeConsensusCount = s.TimeframeConsensusCount;
        vm.OnlyIfLux5m = s.OnlyIfLux5m;
        vm.Lux5mPercentage = s.Lux5mPercentage;
        vm.CheckTrendPrimaryDirection = s.CheckTrendPrimaryDirection;
        vm.TrendPrimaryDirectionCount = s.TrendPrimaryDirectionCount;
        vm.CheckTrendSecondaryDirection = s.CheckTrendSecondaryDirection;
        vm.TrendSecondaryDirectionCount = s.TrendSecondaryDirectionCount;
        vm.CheckPriceAboveMa200 = s.CheckPriceAboveMa200;
        vm.Ma200MinDistancePercentage = s.Ma200MinDistancePercentage;
        vm.Ma200ConfirmationCandles = s.Ma200ConfirmationCandles;
        vm.UseDlzZone = s.UseDlzZone;
        vm.UseFvgZone = s.UseFvgZone;
        vm.UseSmcZone = s.UseSmcZone;
    }

    public void SaveConfig()
    {
        var s = StoRsiPlugin.Settings;

        _viewModel.SoundAndColorsViewModel.SaveConfig(s);
        _viewModel.StrategyEntryConditionsViewModel.SaveConfig(s);

        var vm = _viewModel.StrategyBreSettingsViewModel;
        s.BandLength = vm.BandLength;
        s.OuterMult = vm.OuterMult;
        s.DidoLength = vm.DidoLength;
        s.DidoMult = vm.DidoMult;
        s.UseTrendFilter = vm.UseTrendFilter;
        s.HmaLength = vm.HmaLength;
        s.UseRsiFilter = vm.UseRsiFilter;
        s.RequireStochOsOb = vm.RequireStochOsOb;
        s.AllowStack = vm.AllowStack;
        s.UseStopLoss = vm.UseStopLoss;
        s.TimeframeConsensusCount = vm.TimeframeConsensusCount;
        s.OnlyIfLux5m = vm.OnlyIfLux5m;
        s.Lux5mPercentage = vm.Lux5mPercentage;
        s.CheckTrendPrimaryDirection = vm.CheckTrendPrimaryDirection;
        s.TrendPrimaryDirectionCount = vm.TrendPrimaryDirectionCount;
        s.CheckTrendSecondaryDirection = vm.CheckTrendSecondaryDirection;
        s.TrendSecondaryDirectionCount = vm.TrendSecondaryDirectionCount;
        s.CheckPriceAboveMa200 = vm.CheckPriceAboveMa200;
        s.Ma200MinDistancePercentage = vm.Ma200MinDistancePercentage;
        s.Ma200ConfirmationCandles = vm.Ma200ConfirmationCandles;
        s.UseDlzZone = vm.UseDlzZone;
        s.UseFvgZone = vm.UseFvgZone;
        s.UseSmcZone = vm.UseSmcZone;
    }
}
