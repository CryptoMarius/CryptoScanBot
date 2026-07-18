using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Config.Views;
using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.AtrRb.Config;

public class AtrRbConfigView : IConfigView
{
    private readonly StrategyAtrRbTabViewModel _viewModel = new();

    public string TabHeader => "AtrRb";

    public Control CreateSettingsView()
    {
        return new StrategyAtrRbTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        var s = AtrRbPlugin.Settings;

        _viewModel.SoundAndColorsViewModel.LoadConfig("AtrRb", s);
        _viewModel.StrategyEntryConditionsViewModel.LoadConfig(s);

        var vm = _viewModel.StrategyAtrRbSettingsViewModel;
        vm.Length = s.Length;
        vm.OuterMult = s.OuterMult;
        vm.InnerMult = s.InnerMult;
        vm.BreakLookback = s.BreakLookback;
        vm.UseStopLoss = s.UseStopLoss;
        vm.StopLossAtrFactor = s.StopLossAtrFactor;
        vm.BBMinPercentage = s.BBMinPercentage;
        vm.BBMaxPercentage = s.BBMaxPercentage;
        vm.RequireRsiOsOb = s.RequireRsiOsOb;
        vm.RequireStochOsOb = s.RequireStochOsOb;
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
        var s = AtrRbPlugin.Settings;

        _viewModel.SoundAndColorsViewModel.SaveConfig(s);
        _viewModel.StrategyEntryConditionsViewModel.SaveConfig(s);

        var vm = _viewModel.StrategyAtrRbSettingsViewModel;
        s.Length = vm.Length;
        s.OuterMult = vm.OuterMult;
        s.InnerMult = vm.InnerMult;
        s.BreakLookback = vm.BreakLookback;
        s.UseStopLoss = vm.UseStopLoss;
        s.StopLossAtrFactor = vm.StopLossAtrFactor;
        s.BBMinPercentage = vm.BBMinPercentage;
        s.BBMaxPercentage = vm.BBMaxPercentage;
        s.RequireRsiOsOb = vm.RequireRsiOsOb;
        s.RequireStochOsOb = vm.RequireStochOsOb;
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
