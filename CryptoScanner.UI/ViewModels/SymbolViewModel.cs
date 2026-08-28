using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.UI.ViewModels;

public class SymbolViewModel
{
    public CryptoSymbol Object { get; }

    public SymbolViewModel(CryptoSymbol symbol)
    {
        Object = symbol;
    }

    // Immutable (never change after creation)
    public string Symbol => Object.PairName;

    // Which market inside the exchange, shown as a coloured badge behind the name
    public string MarketLabel => Object.MarketLabel;

    // The name the instrument has at the exchange, for the hidden debug column. Not the scanner
    // name: "BTC-USDT-SWAP" against "BTCUSDT", and for an X-Perp the two look nothing alike.
    public string ExchangeName => Object.ExchangeName;

    private string? _idText;
    public string Id
    {
        get
        {
            _idText ??= Object.Id.ToString();
            return _idText!;
        }
    }

    private string? _backgroundStyle;
    // The colour of a basecoin can be changed in the settings, so the cache needs a way to know it
    // is stale. Without this the symbol grid kept painting the colour it had at startup.
    private int _backgroundStyleVersion = -1;
    public string BackgroundStyle
    {
        get
        {
            if (_backgroundStyle == null || _backgroundStyleVersion != ColorHelper.QuoteColorVersion)
            {
                _backgroundStyle = ColorHelper.GetBackgroundStyle(Object.QuoteData);
                _backgroundStyleVersion = ColorHelper.QuoteColorVersion;
            }
            return _backgroundStyle!;
        }
    }

    // Cached with invalidation
    private string? _volumeText;
    public string VolumeText
    {
        get
        {
            _volumeText ??= Object.Volume.ToString("N0");
            return _volumeText!;
        }
    }

    private string? _volumeColorClass;
    public string VolumeColorClass
    {
        get
        {
            _volumeColorClass ??= ColorHelper.GetVolumeColorClass(Object, Object.Volume);
            return _volumeColorClass!;
        }
    }

    private string? _distanceText;
    public string DistanceText
    {
        get
        {
            _distanceText ??= ZoneTools.ZoneDistance(Object).ToString0("N2");
            return _distanceText!;
        }
    }

    public void InvalidateVolume()
    {
        _volumeText = null;
        _volumeColorClass = null;
    }

    public void InvalidateDistance()
    {
        _distanceText = null;
    }

    public void InvalidateAll()
    {
        _volumeText = null;
        _volumeColorClass = null;
        _distanceText = null;
    }
}
