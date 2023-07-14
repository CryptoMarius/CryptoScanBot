namespace CryptoSbmScanner.Exchange;

public abstract class UserDataBase
{
    public abstract Task Start();
    public abstract Task Stop();
    public abstract void Reset();
    public abstract int Count();
}

