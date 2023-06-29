namespace CryptoSbmScanner.Enums;

public enum CryptoBuyStepInMethod
{
    Immediately, // Direct instappen
    FixedPercentage, // De Zignally manier (bij elke dca verdubbeld de investering)
    AfterNextSignal, // Stap op een volgende melding in (rekening houdende met cooldown en percentage)
    TrailViaKcPsar // stop limit buy op de bovenste KC/PSAR
}
