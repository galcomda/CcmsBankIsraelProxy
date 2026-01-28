namespace CcmsBankIsraelProxy.Models;

/// <summary>
/// Employee data returned from BOI SAP system (comda_res_dtEmpData)
/// </summary>
public class EmployeeData
{
    public string? IdNum { get; set; }
    public string? EmpNum { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FirstNameEng { get; set; }
    public string? LastNameEng { get; set; }
    public string? HireDate { get; set; }
    public string? FireDate { get; set; }
    public string? EmpType { get; set; }
    public string? CardNum { get; set; }
    public string? Image { get; set; }
    public string? UPN { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Status { get; set; }
    public string? StatusDesc { get; set; }
}
