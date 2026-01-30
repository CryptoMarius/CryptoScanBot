using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Messages;

public class PositionIsCreatedMessage
{
    public CryptoPosition Position { get; }

    public PositionIsCreatedMessage(CryptoPosition position)
    {
        Position = position;
    }
}
