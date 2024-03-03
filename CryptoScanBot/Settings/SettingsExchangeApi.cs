using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoScanBot.Settings;

[Serializable]
public class SettingsExchangeApi
{
    // TODO: Iedere exchange heeft 0 of meer key/secret's
    // (ze moeten ook nog ff versleuteld worden lijkt me)
    // Liefst in database zodat ze niet leesbaar in json staan
    // https://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp
    public string Key { get; set; } = "";
    public string Secret { get; set; } = "";
    public string PassPhrase { get; set; } = ""; // Kucoin
}