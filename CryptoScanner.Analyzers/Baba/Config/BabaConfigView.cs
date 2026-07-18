using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Config.Views;
using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Baba.Config;

public class BabaConfigView : IConfigView
{
    private readonly StrategyBabaTabViewModel _viewModel = new();

    public string TabHeader => "Baba";

    public Control CreateSettingsView()
    {
        return new StrategyBabaTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        var s = BabaPlugin.Settings;

        _viewModel.SoundAndColorsViewModel.LoadConfig("Baba", s);
        _viewModel.StrategyEntryConditionsViewModel.LoadConfig(s);

        var vm = _viewModel.StrategyBabaSettingsViewModel;
        vm.Length = s.Length;
        vm.Mult = s.Mult;
        vm.AtrLength = s.AtrLength;
        vm.AtrMult = s.AtrMult;
        vm.UseRsiFilter = s.UseRsiFilter;
        vm.UseSlideFilter = s.UseSlideFilter;
        vm.SlideWindow = s.SlideWindow;
        vm.SlideMinEfficiency = s.SlideMinEfficiency;
        vm.SlideMinMovePercent = s.SlideMinMovePercent;
        vm.UseStopLoss = s.UseStopLoss;
        vm.SLStdevFactor = s.SLStdevFactor;
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
        var s = BabaPlugin.Settings;

        _viewModel.SoundAndColorsViewModel.SaveConfig(s);
        _viewModel.StrategyEntryConditionsViewModel.SaveConfig(s);

        var vm = _viewModel.StrategyBabaSettingsViewModel;
        s.Length = vm.Length;
        s.Mult = vm.Mult;
        s.AtrLength = vm.AtrLength;
        s.AtrMult = vm.AtrMult;
        s.UseRsiFilter = vm.UseRsiFilter;
        s.UseSlideFilter = vm.UseSlideFilter;
        s.SlideWindow = vm.SlideWindow;
        s.SlideMinEfficiency = vm.SlideMinEfficiency;
        s.SlideMinMovePercent = vm.SlideMinMovePercent;
        s.UseStopLoss = vm.UseStopLoss;
        s.SLStdevFactor = vm.SLStdevFactor;
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
