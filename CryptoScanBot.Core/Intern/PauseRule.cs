namespace CryptoScanBot.Core.Intern;

//***************************
// Pauseren van trading als een of meerdere munten (sterk) bewegen of de barometer beweegt

public class PauseRule
{
    public DateTime? Calculated { get; set; }
    public DateTime? Until { get; set; }
    public string Text { get; set; }
}
