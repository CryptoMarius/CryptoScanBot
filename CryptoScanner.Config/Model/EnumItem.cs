namespace CryptoScanner.Config.Model;

public class EnumItem<T>
{
    public T Value { get; set; }
    public string Display { get; set; }

    public EnumItem(T value, string display)
    {
        Value = value;
        Display = display;
    }
}
