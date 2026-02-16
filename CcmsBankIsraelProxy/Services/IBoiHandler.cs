using CcmsBankIsraelProxy.Models;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Main handler for BOI operations - orchestrates SAP picture and SRHR updates
/// </summary>
public interface IBoiHandler
{
    Task<(bool success, string message)> HandleBoiCallback(Dictionary<string, object> cardData, Operations operation);
}
