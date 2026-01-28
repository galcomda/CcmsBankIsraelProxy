namespace CcmsBankIsraelProxy.Models;

public class CallbackResponse
{
    public bool IsValid { get; set; } = true;
    public string? Message { get; set; }
    public string? CardData { get; set; }
}
