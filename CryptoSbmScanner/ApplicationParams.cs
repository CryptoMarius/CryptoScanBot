using CommandLine;

namespace CryptoSbmScanner;

// Define a class to receive parsed values
class ApplicationParams
{
    static public ApplicationParams Options { get; set; }


    [Option('f', "folder", Required = false, HelpText = "De te gebruiken folder in de APPDATA")]
    public string InputFile { get; set; } = "CryptoScanner";

    //[Option('v', "verbose", HelpText = "Prints all messages to standard output.")] //DefaultValue = true, 
    //public bool Verbose { get; set; } = true;

}
