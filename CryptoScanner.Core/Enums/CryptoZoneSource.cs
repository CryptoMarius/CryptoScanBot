namespace CryptoScanner.Core.Enums;

/// <summary>
/// Where a zone comes from, named after the three zone strategies that produce them.
/// <para>
/// Deliberately not <see cref="CryptoZoneKind"/>: that names what a zone IS (a dominant level, a
/// fair value gap, an order block), while a setting has to name the block that produces it - the
/// intervals live under Signal.ZonesDlz, ZonesFvg and ZonesSmc, one list per source.
/// </para>
/// <para>
/// The member names are the values that end up in the settings file and in the emulator queue.
/// Both are read case-insensitively, so the "dlz" written there by hand keeps matching.
/// </para>
/// </summary>
public enum CryptoZoneSource
{
    Dlz = 1,
    Fvg = 2,
    Smc = 3,
}
