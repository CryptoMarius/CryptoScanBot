namespace CryptoScanner.Core.Model
{
    // For display in the symbol grid
    // These are the closest zones
    // (calculated from all the interval zones)
    public class CryptoZoneDistance
    {
        /// <summary>
        /// The distance to nearest long zone (percentage)
        /// </summary>
        public decimal? BestLongZone { get; internal set; } = 100m;
        /// <summary>
        /// The distance to nearest short zone (percentage)
        /// </summary>
        public decimal? BestShortZone { get; internal set; } = 100m;
    }

}