using System.Security.Cryptography;
using System.Text;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

[Table("Exchange")]
public class CryptoExchange
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }

    [Computed]
    // Datum dat de laatste keer de exchange informatie is opgehaald
    public DateTime ExchangeInfoLastTime { get; set; } = DateTime.MinValue;


    // Coins indexed on id and name
    // (Altrady BINA:SymbolName)
    [Computed]
    public SortedList<int, CryptoSymbol> SymbolListId { get; } = new();
    [Computed]
    public SortedList<string, CryptoSymbol> SymbolListName { get; } = new();


    [Computed]
    static private string Text { get { return "Yes, man is mortal, but that would be only half the trouble. The worst of it is that he's sometimes unexpectedly mortal—there's the trick!"; } }

    
    public static string Encrypt(string toEncrypt, bool useHashing)
    {
        byte[] keyArray;
        byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

        //System.Configuration.AppSettingsReader settingsReader = new AppSettingsReader();
        // Get the key from config file

        string key = Text;
        //System.Windows.Forms.MessageBox.Show(key);
        //If hashing use get hashcode regards to your key
        if (useHashing)
        {
            //MD5CryptoServiceProvider hashmd5 = new();
            MD5 hashmd5 = MD5.Create();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            //Always release the resources and flush data
            // of the Cryptographic service provide. Best Practice

            hashmd5.Clear();
        }
        else
            keyArray = UTF8Encoding.UTF8.GetBytes(key);

        //TripleDESCryptoServiceProvider tdes = new();
        TripleDES tdes = TripleDES.Create();
        //set the secret key for the tripleDES algorithm
        tdes.Key = keyArray;
        //mode of operation. there are other 4 modes.
        //We choose ECB(Electronic code Book)
        tdes.Mode = CipherMode.ECB;
        //padding mode(if any extra byte added)

        tdes.Padding = PaddingMode.PKCS7;

        ICryptoTransform cTransform = tdes.CreateEncryptor();
        //transform the specified region of bytes array to resultArray
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

        //Release resources held by TripleDes Encryptor
        tdes.Clear();

        //Return the encrypted data into unreadable string format
        return Convert.ToBase64String(resultArray, 0, resultArray.Length);
    }


    public static string Decrypt(string cipherString, bool useHashing)
    {
        if (string.IsNullOrEmpty(cipherString))
            return string.Empty;

        byte[] keyArray;
        //get the byte code of the string
        byte[] toEncryptArray = Convert.FromBase64String(cipherString);

        string key = Text;

        if (useHashing)
        {
            //if hashing was used get the hash code with regards to your key
            //MD5CryptoServiceProvider hashmd5 = new();
            MD5 hashmd5 = MD5.Create();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            //release any resource held by the MD5CryptoServiceProvider

            hashmd5.Clear();
        }
        else
        {
            //if hashing was not implemented get the byte code of the key
            keyArray = UTF8Encoding.UTF8.GetBytes(key);
        }

        //TripleDESCryptoServiceProvider tdes = new();
        TripleDES tdes = TripleDES.Create();
        //set the secret key for the tripleDES algorithm
        tdes.Key = keyArray;
        //mode of operation. there are other 4 modes. 
        //We choose ECB(Electronic code Book)

        tdes.Mode = CipherMode.ECB;
        //padding mode(if any extra byte added)
        tdes.Padding = PaddingMode.PKCS7;

        ICryptoTransform cTransform = tdes.CreateDecryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

        //Release resources held by TripleDes Encryptor                
        tdes.Clear();
        //return the Clear decrypted TEXT
        return UTF8Encoding.UTF8.GetString(resultArray);
    }

}