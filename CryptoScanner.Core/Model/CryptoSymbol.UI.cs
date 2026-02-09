using Avalonia.Media;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Zones;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

using static CryptoScanner.Core.Model.CryptoDisplayHelpers;

public partial class CryptoSymbol
{
    [Computed]
    public string IdText => Id.ToString();

    [Computed]
    public string SymbolText => Name;
    [Computed]
    public IBrush SymbolBackground => new SolidColorBrush(QuoteData.DisplayColor);

    [Computed]
    public string VolumeText => Volume.ToString("N0");
    [Computed]
    public IBrush VolumeForeground => GetVolumeColor(this, Volume);

    [Computed]
    public string DistanceText => ZoneTools.ZoneDistance(this).ToString0("N2") ?? "100";
}