using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Messages;

public class PositionIsClosedMessage
{
    public CryptoPosition Position { get; }

    public PositionIsClosedMessage(CryptoPosition position)
    {
        Position = position;
    }
}
