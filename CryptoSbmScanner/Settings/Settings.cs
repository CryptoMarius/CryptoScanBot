using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoSbmScanner
{
    static public class Constants
    {
        public const string SymbolNameBarometerPrice = "$BMP";
    }



    //De instellingen die het analyse gedeelte nodig heeft
    [Serializable]
    public class Settings
    {
        //Welke basis munten willen we gebruiken
        public SortedList<string, CryptoQuoteData> QuoteCoins { get; } = new SortedList<string, CryptoQuoteData>();

        /// <summary>
        ///  Standaard instellingen
        /// </summary>
        public SettingsGeneral General { get; set; } = new SettingsGeneral();

        /// <summary>
        ///  Signal gerelateerde instellingen
        /// </summary>
        public SettingsSignal Signal { get; set; } = new SettingsSignal();


        // Als dit aan staat moet de symbol staat in de whitelist dan wordt het toegestaan
        public bool UseWhiteListOversold { get; set; } = false;
        public List<string> WhiteListOversold = new List<string>();

        // Als dit aan en de symbol staat in de blacklist dan wordt de symbol overgeslagen
        public bool UseBlackListOversold { get; set; } = false;
        public List<string> BlackListOversold = new List<string>();



        // Als dit aan staat moet de symbol staat in de whitelist dan wordt het toegestaan
        public bool UseWhiteListOverbought { get; set; } = false;
        public List<string> WhiteListOverbought = new List<string>();

        // Als dit aan en de symbol staat in de blacklist dan wordt de symbol overgeslagen
        public bool UseBlackListOverbought { get; set; } = false;
        public List<string> BlackListOverbought = new List<string>();


        /// <summary>
        /// De basis instellingen voor de Settings
        /// </summary>
        public Settings()
        {
        }

    }   

}
