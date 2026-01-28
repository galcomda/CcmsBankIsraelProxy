namespace CcmsBankIsraelProxy.Models;

public class CallbackData
{
    public int Id { get; set; }
    public int Operation { get; set; }
    public int EntityTypeId { get; set; }
    public int EntityId { get; set; }
    public string? Issuer { get; set; }
    public string? PreviousCardData { get; set; }
    public string CardData { get; set; } = string.Empty;
}
