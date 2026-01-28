using CcmsBankIsraelProxy.Models;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Service for interacting with BOI SAP Employee interface (comda_mi)
/// </summary>
public interface ISapEmployeeService
{
    /// <summary>
    /// Gets employee data from SAP by ID number
    /// </summary>
    Task<(bool success, EmployeeData? employee, string message)> GetEmployeeByIdAsync(string idNumber);
}
