namespace CcmsBankIsraelProxy.Models;

/// <summary>
/// Configuration mapping for CCMS card fields to BOI system fields
/// </summary>
public class BoiCardFields
{
    // Basic identification
    public string IdNumber { get; set; } = "ID_Number";
    public string EmployeeNumber { get; set; } = "Employee_Number";
    public string CardNumber { get; set; } = "CardNo";
    
    // Personal details
    public string FirstName { get; set; } = "FirstName_Hb";
    public string LastName { get; set; } = "LastName_Hb";
    public string FirstNameEng { get; set; } = "FirstName_En";
    public string LastNameEng { get; set; } = "LastName_En";
    
    // Employment details
    public string EmployeeType { get; set; } = "EmployeeType";
    public string HireDate { get; set; } = "HireDate";
    public string FireDate { get; set; } = "FireDate";
    
    // Contact info
    public string PhoneNumber { get; set; } = "Phone";
    public string Email { get; set; } = "Email";
    public string UPN { get; set; } = "UPN";
    
    // Photo
    public string Photo { get; set; } = "Photo";
    public string PhotoBase64 { get; set; } = "PhotoBase64";
}
