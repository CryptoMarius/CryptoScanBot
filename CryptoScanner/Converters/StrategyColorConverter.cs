using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

using CryptoScanner.ViewModels;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Globalization;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Converters
{
    // Background symbol based on the strategy
    public class StrategyColorConverter : ColorConverter, IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            //Avalonia 11 used the folloowing resource‑keys for DataGrid‑selection:
            //Selectie‑achtergrond DataGridRowSelectedBackground
            //Selectie‑foreground DataGridRowSelectedForeground
            //Hover‑kleur DataGridRowPointerOverBackground

            var isSelected = values[1] as bool? ?? false;
            if (isSelected)
                return Application.Current?.FindResource("DataGridRowSelectedBackground");

            var isHover = values[2] as bool? ?? false;
            if (isHover)
                return Application.Current?.FindResource("DataGridRowPointerOverBackground");

            CryptoTradeSide? tradeSide = null;
            CryptoSignalStrategy? strategy = null;

            var value = values[0];
            if (value is SignalViewModel signal)
            {
                tradeSide = signal.Object.Side;
                strategy = signal.Object.Strategy;
            }
            //else if (value is SymbolViewModel symbol)
            //{
            //}
            //else if (value is LiveDataViewModel liveData)
            //{
            //}
            else if (value is PositionViewModel position)
            {
                tradeSide = position.Object.Side;
                strategy = position.Object.Strategy;
            }

            if (strategy != null && tradeSide != null)
            {
                if (GlobalData.StrategiesSettings.TryGetValue(strategy.Value, out (SettingsSignalStrategyBase strategySettings, long lastSignalTime) x))
                {
                    if (tradeSide == CryptoTradeSide.Long)
                        return new SolidColorBrush(x.strategySettings.ColorLong);
                    else
                        return new SolidColorBrush(x.strategySettings.ColorShort);
                }
            }

            return Brushes.Transparent;
        }
    }
}