namespace CryptoScanBot.Core.Enums;

// for both entry or dca
public enum CryptoEntryOrDcaStrategy
{
    AfterNextSignal, // Stap op een melding in (rekening houdende met cooldown en percentage indien DCA)
    FixedPercentage, // Stap in op een gefixeerde percentage (rekening houdende met cooldown en percentage indien DCA)
    TrailViaKcPsar // Stap in na een signaal of fixed percentage en ga dan boven (of beneden) trailen
}
