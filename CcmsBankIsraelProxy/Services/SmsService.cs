using System.Net;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Implementation of BOI SMS SOAP service (SendSMS from sms.wsdl)
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private readonly string _smsEndpoint;

    public SmsService(
        ILogger<SmsService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _smsEndpoint = configuration.GetValue<string>("Boi:Sms:Endpoint")
            ?? "http://iiscrtprd/shvakim/GeneralServices/service1.asmx";

        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<(bool success, string message)> SendSmsAsync(string toNumbers, string message)
    {
        _logger.LogInformation("========== SMS Send Request ==========");
        _logger.LogInformation("Sending SMS to: {ToNumbers}", toNumbers);
        _logger.LogDebug("Message: {Message}", message);

        if (!_configuration.GetValue("Boi:Sms:Enabled", true))
        {
            _logger.LogWarning("SMS service is DISABLED in configuration");
            return (false, "SMS service is disabled");
        }

        if (string.IsNullOrEmpty(toNumbers))
        {
            _logger.LogWarning("No phone number provided");
            return (false, "No phone number provided");
        }

        if (string.IsNullOrEmpty(message))
        {
            _logger.LogWarning("No message provided");
            return (false, "No message provided");
        }

        try
        {
            // Build SOAP envelope for SendSMS
            string soapEnvelope = BuildSendSmsSoapEnvelope(toNumbers, message);

            _logger.LogDebug("SOAP Request to: {Endpoint}", _smsEndpoint);
            _logger.LogTrace("SOAP Envelope: {Envelope}", soapEnvelope);

            using var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://tempuri.org/SendSMS");

            var response = await _httpClient.PostAsync(_smsEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Response Status: {StatusCode}", response.StatusCode);
            _logger.LogTrace("Response Body: {Body}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SMS HTTP Error {StatusCode}: {Body}", response.StatusCode, responseBody);
                return (false, $"SMS service returned error: {response.StatusCode}");
            }

            // Parse response
            var (success, result) = ParseSendSmsResponse(responseBody);

            if (success)
            {
                _logger.LogInformation("SMS sent SUCCESS to: {ToNumbers}", toNumbers);
            }
            else
            {
                _logger.LogError("SMS send FAILED to: {ToNumbers} - {Result}", toNumbers, result);
            }

            return (success, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in SendSmsAsync: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }

    private static string BuildSendSmsSoapEnvelope(string toNumbers, string message)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:tns=""http://tempuri.org/"">
    <soap:Body>
        <tns:SendSMS>
            <tns:toNumbers>{SecurityElement.Escape(toNumbers)}</tns:toNumbers>
            <tns:message>{SecurityElement.Escape(message)}</tns:message>
        </tns:SendSMS>
    </soap:Body>
</soap:Envelope>";
    }

    private (bool success, string message) ParseSendSmsResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);

            // Find SendSMSResult element
            var resultElement = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "SendSMSResult");

            var result = resultElement?.Value ?? string.Empty;

            // Consider empty or null result as success (common pattern for void methods)
            // Check if result contains error indicators
            if (result.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                result.Contains("fail", StringComparison.OrdinalIgnoreCase))
            {
                return (false, result);
            }

            return (true, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse SMS response: {Message}", ex.Message);
            return (true, string.Empty); // Assume success if we can't parse
        }
    }
}
