using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

public partial class CryptoLog
{
    [Computed]
    public string DateText => Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    [Computed]
    public string LineText => Text;
}
