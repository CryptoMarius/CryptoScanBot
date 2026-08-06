namespace CryptoScanner.Core.Enums;

public enum CryptoPositionStatus
{
    Waiting, // 0
    Trading, // 1
    Ready, // 2
    Timeout, // 3
    TakeOver, // 4
    Altrady, // 5
    Cancelled, // 6 new ignal invalidates the waiting position
}
