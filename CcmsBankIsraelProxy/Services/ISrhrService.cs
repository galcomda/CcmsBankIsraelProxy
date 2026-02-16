using CcmsBankIsraelProxy.Models;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Service for updating employee data in SRHR system
/// </summary>
public interface ISrhrService
{
    Task<(bool success, string message)> UpdatePolimilAsync(PolimilRequest request);
}
