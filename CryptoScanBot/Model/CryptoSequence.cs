namespace CryptoScanBot.Model;

using Dapper.Contrib.Extensions;

[Table("Sequence")]
public class CryptoSequence
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
}