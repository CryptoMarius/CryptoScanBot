using CryptoScanner.Core.Exchange.Altrady;

namespace CryptoScanner.Commands;

/// <summary>
/// Hand the selected signal to Altrady as a prefilled trade setup; the user confirms it in Altrady.
/// </summary>
public class CommandAltradyTradeSetup : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.signal != null)
            AltradyTradeSetup.Send(dto.signal);
    }
}
