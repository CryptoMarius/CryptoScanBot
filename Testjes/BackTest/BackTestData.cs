using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.BackTest;

///// <summary>
///// Persistent data voor een symbol (op een bepaald interval), signaal (van een interval) of een positie (die net start)?
///// </summary>
//public class BackTestData
//{
//    // Trading or Emulator
//    public int OrderId = 0;
//    public int DcaIndex = 0;
//    public int BuyTimeOut = 0;

//    // lijkt me overbodig omdat het nu vanuit een posutie wordt beredeneerd
//    public CryptoPosition Position;

//    // De laatste prijs waarop (bij)gekocht werd
//    public decimal LastBuyPrice;

//    public decimal PriceLastOversold; // De laagste prijs van een oversold

//    //public void Reset()
//    //{
//    //    // Trading or Emulator
//    //    OrderId = 0;
//    //    DcaIndex = 0;
//    //    BuyTimeOut = 0;

//    //    Position = null;
//    //}
//}