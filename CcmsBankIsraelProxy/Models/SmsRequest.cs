namespace CcmsBankIsraelProxy.Models;

/// <summary>
/// Request to send SMS via BOI SMS service
/// </summary>
public class SmsRequest
{
    /// <summary>
    /// Phone number(s) to send to (can be comma separated for multiple)
    /// </summary>
    public string ToNumbers { get; set; } = string.Empty;
    
    /// <summary>
    /// SMS message content
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response from SMS service
/// </summary>
public class SmsResponse
{
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
