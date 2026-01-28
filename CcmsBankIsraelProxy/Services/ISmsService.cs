using CcmsBankIsraelProxy.Models;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Service for sending SMS via BOI SMS service
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends SMS message to specified phone number(s)
    /// </summary>
    Task<(bool success, string message)> SendSmsAsync(string toNumbers, string message);
}
