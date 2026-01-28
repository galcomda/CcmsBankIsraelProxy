using CcmsBankIsraelProxy.Models;
using System.Net;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Implementation of BOI SAP Employee SOAP service (comda_mi from comda_employee_test.wsdl)
/// </summary>
public class SapEmployeeService : ISapEmployeeService
{
    private readonly ILogger<SapEmployeeService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private readonly string _sapEndpoint;
    private readonly string _sapUsername;
    private readonly string _sapPassword;

    public SapEmployeeService(
        ILogger<SapEmployeeService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        _sapEndpoint = configuration.GetValue<string>("Boi:Sap:Endpoint") 
            ?? "http://localhost:14363/SAP_IN.asmx";
        _sapUsername = configuration.GetValue<string>("Boi:Sap:Username") ?? "";
        _sapPassword = configuration.GetValue<string>("Boi:Sap:Password") ?? "";

        _httpClient = httpClientFactory.CreateClient();
        
        // Setup Basic Auth if credentials provided
        if (!string.IsNullOrEmpty(_sapUsername))
        {
            var authBytes = Encoding.ASCII.GetBytes($"{_sapUsername}:{_sapPassword}");
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        }
    }

    public async Task<(bool success, EmployeeData? employee, string message)> GetEmployeeByIdAsync(string idNumber)
    {
        _logger.LogInformation("========== SAP GetEmployee Request ==========");
        _logger.LogInformation("Looking up employee by ID: {IdNumber}", idNumber);

        if (!_configuration.GetValue("Boi:Sap:Enabled", true))
        {
            _logger.LogWarning("SAP service is DISABLED in configuration");
            return (false, null, "SAP service is disabled");
        }

        try
        {
            // Build SOAP envelope for comda_mi
            string soapEnvelope = BuildGetEmployeeSoapEnvelope(idNumber);
            
            _logger.LogDebug("SOAP Request to: {Endpoint}", _sapEndpoint);
            _logger.LogTrace("SOAP Envelope: {Envelope}", soapEnvelope);

            using var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://boi.sap.in/comda_mi");

            var response = await _httpClient.PostAsync(_sapEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Response Status: {StatusCode}", response.StatusCode);
            _logger.LogTrace("Response Body: {Body}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SAP HTTP Error {StatusCode}: {Body}", response.StatusCode, responseBody);
                return (false, null, $"SAP returned error: {response.StatusCode}");
            }

            // Parse SOAP response
            var employee = ParseGetEmployeeResponse(responseBody);
            
            if (employee == null)
            {
                _logger.LogWarning("Employee not found for ID: {IdNumber}", idNumber);
                return (false, null, "Employee not found");
            }

            _logger.LogInformation("Employee found: {EmpNum} - {FirstName} {LastName}", 
                employee.EmpNum, employee.FirstName, employee.LastName);
            
            return (true, employee, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetEmployeeByIdAsync: {Message}", ex.Message);
            return (false, null, ex.Message);
        }
    }

    private static string BuildGetEmployeeSoapEnvelope(string idNumber)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:tns=""http://boi.sap.in/"" 
               xmlns:s1=""http://boi.org.il/HR_Comda"">
    <soap:Body>
        <tns:comda_mi>
            <tns:dt>
                <IDNum>{SecurityElement.Escape(idNumber)}</IDNum>
            </tns:dt>
        </tns:comda_mi>
    </soap:Body>
</soap:Envelope>";
    }

    private EmployeeData? ParseGetEmployeeResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);
            
            // Define namespaces
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace tns = "http://boi.sap.in/";
            XNamespace s1 = "http://boi.org.il/HR_Comda";

            // Find the employee data element
            var empDataElement = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "comda_res_dtEmpData");

            if (empDataElement == null)
            {
                _logger.LogDebug("No comda_res_dtEmpData element found in response");
                return null;
            }

            // Helper to get element value
            string? GetValue(string elementName) => 
                empDataElement.Elements().FirstOrDefault(e => e.Name.LocalName == elementName)?.Value;

            return new EmployeeData
            {
                IdNum = GetValue("IdNum"),
                EmpNum = GetValue("EmpNum"),
                FirstName = GetValue("FirstName"),
                LastName = GetValue("LastName"),
                FirstNameEng = GetValue("FIRST_NAME_ENG"),
                LastNameEng = GetValue("LAST_NAME_ENG"),
                HireDate = GetValue("HierDate"),
                FireDate = GetValue("FireDate"),
                EmpType = GetValue("EmpType"),
                CardNum = GetValue("CardNum"),
                Image = GetValue("IMAGE"),
                UPN = GetValue("UPN"),
                PhoneNumber = GetValue("PHONE_NUMBER"),
                Status = GetValue("Status"),
                StatusDesc = GetValue("StatusDesc")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse SAP response: {Message}", ex.Message);
            return null;
        }
    }
}
