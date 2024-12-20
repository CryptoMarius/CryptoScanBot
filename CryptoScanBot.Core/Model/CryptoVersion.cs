using Dapper.Contrib.Extensions;


[Table("Version")]
public class CryptoVersion
{
    [Key]
    public int Id { get; set; }
    public int Version { get; set; }
}