using CcmsBankIsraelProxy.Models;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Main handler for BOI operations - orchestrates SAP, Picture, and SMS services
/// </summary>
public interface IBoiHandler
{
    /// <summary>
    /// Handles CCMS callback - processes employee data and updates BOI systems
    /// </summary>
    Task<(bool success, string message)> HandleBoiCallback(Dictionary<string, object> cardData, Operations operation);
    
    /// <summary>
    /// Gets employee data from BOI SAP
    /// </summary>
    Task<(bool success, EmployeeData? employee, string message)> GetEmployee(string idNumber);
    
    /// <summary>
    /// Updates employee picture in BOI SAP
    /// </summary>
    Task<(bool success, string message)> UpdatePicture(string idNumber, string cardNumber, string pictureBase64);
    
    /// <summary>
    /// Sends SMS via BOI SMS service
    /// </summary>
    Task<(bool success, string message)> SendSms(string phoneNumber, string message);
}
