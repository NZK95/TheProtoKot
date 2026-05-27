internal sealed class BlockedPlayer
{
    public string Sheet { get; set; } = null!;
    public int? SheetRow { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Reporter { get; set; } = null!;   
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string DateEntry { get; set; } = null!;
    public string DateEnd { get; set; } = null!;
   // public string VK { get; set; } = null!;
    public string MultipleAccounts { get; set; } = null!;
    public string Extender { get; set; } = null!;
}
