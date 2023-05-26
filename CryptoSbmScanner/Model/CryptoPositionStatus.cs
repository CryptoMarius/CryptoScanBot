namespace CryptoSbmScanner.Model;

public enum CryptoPositionStatus
{
    positionWaiting, // 0
    positionTrading, // 1
    positionReady, // 2
    positionTimeout, // 3
    positionTakeOver // 4
}
