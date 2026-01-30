using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Messages;

public class PositionIsDeletedMessage
{
    public CryptoPosition Position { get; }

    public PositionIsDeletedMessage(CryptoPosition position)
    {
        Position = position;
    }
}
