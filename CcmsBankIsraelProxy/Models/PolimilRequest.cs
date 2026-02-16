namespace CcmsBankIsraelProxy.Models;

/// <summary>
/// SRHR Polimil request matching BOI_WS Polimil structure
/// </summary>
public class PolimilRequest
{
    public string CardId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeType { get; set; } = string.Empty;
    public string Factory { get; set; } = "0000";
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public byte[]? Image { get; set; }
}
