using CcmsBankIsraelProxy.Models;
using System.Net;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Implementation of BOI SAP Picture update SOAP service (poli_pic_mi from comda_pic.wsdl)
/// </summary>
public class PictureService : IPictureService
{
    private readonly ILogger<PictureService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private readonly string _pictureEndpoint;
    private readonly string _sapUsername;
    private readonly string _sapPassword;

    public PictureService(
        ILogger<PictureService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _pictureEndpoint = configuration.GetValue<string>("Boi:Picture:Endpoint")
            ?? "https://sapwaexq.ad.boi.gov.il:50001/XISOAPAdapter/MessageServlet";
        _sapUsername = configuration.GetValue<string>("Boi:Picture:Username") ?? "";
        _sapPassword = configuration.GetValue<string>("Boi:Picture:Password") ?? "";

        _httpClient = httpClientFactory.CreateClient();

        // Setup Basic Auth if credentials provided
        if (!string.IsNullOrEmpty(_sapUsername))
        {
            var authBytes = Encoding.ASCII.GetBytes($"{_sapUsername}:{_sapPassword}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        }
    }

    public async Task<(bool success, string message)> UpdatePictureAsync(PictureUpdateRequest request)
    {
        _logger.LogInformation("========== Picture Update Request ==========");
        _logger.LogInformation("Updating picture for ID: {IdNum}, CardNum: {CardNum}", request.IdNum, request.CardNum);

        if (!_configuration.GetValue("Boi:Picture:Enabled", true))
        {
            _logger.LogWarning("Picture service is DISABLED in configuration");
            return (false, "Picture service is disabled");
        }

        if (string.IsNullOrEmpty(request.Picture))
        {
            _logger.LogWarning("No picture data provided");
            return (false, "No picture data provided");
        }

        try
        {
            // Build SOAP envelope for poli_pic_mi
            string soapEnvelope = BuildUpdatePictureSoapEnvelope(request);

            _logger.LogDebug("SOAP Request to: {Endpoint}", _pictureEndpoint);
            _logger.LogDebug("Picture size: {Size} bytes (base64)", request.Picture?.Length ?? 0);

            using var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://sap.com/xi/WebService/soap1.1");

            var response = await _httpClient.PostAsync(_pictureEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Response Status: {StatusCode}", response.StatusCode);
            _logger.LogTrace("Response Body: {Body}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Picture HTTP Error {StatusCode}: {Body}", response.StatusCode, responseBody);
                return (false, $"SAP returned error: {response.StatusCode}");
            }

            // Parse response for any error messages
            var (success, message) = ParseUpdatePictureResponse(responseBody);

            if (success)
            {
                _logger.LogInformation("Picture update SUCCESS for ID: {IdNum}", request.IdNum);
            }
            else
            {
                _logger.LogError("Picture update FAILED for ID: {IdNum} - {Message}", request.IdNum, message);
            }

            return (success, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in UpdatePictureAsync: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }

    private static string BuildUpdatePictureSoapEnvelope(PictureUpdateRequest request)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:p2=""http://boi.org.il/HR_Polimil_Pic"">
    <soap:Body>
        <p2:comda_pic_req_mt>
            <p2:IDNum>{SecurityElement.Escape(request.IdNum)}</p2:IDNum>
            <p2:CardNum>{SecurityElement.Escape(request.CardNum)}</p2:CardNum>
            <p2:Picture>{request.Picture}</p2:Picture>
        </p2:comda_pic_req_mt>
    </soap:Body>
</soap:Envelope>";
    }

    private (bool success, string message) ParseUpdatePictureResponse(string soapResponse)
    {
        try
        {
            var doc = XDocument.Parse(soapResponse);

            // Find ReturnMessage elements
            var returnMessages = doc.Descendants()
                .Where(e => e.Name.LocalName == "ReturnMessage")
                .ToList();

            if (returnMessages.Count == 0)
            {
                // No error messages means success
                return (true, string.Empty);
            }

            // Check for error messages
            var errors = new List<string>();
            foreach (var msg in returnMessages)
            {
                var type = msg.Elements().FirstOrDefault(e => e.Name.LocalName == "Type")?.Value;
                var message = msg.Elements().FirstOrDefault(e => e.Name.LocalName == "Message")?.Value;

                if (type == "E" || type == "Error")
                {
                    errors.Add(message ?? "Unknown error");
                }
            }

            if (errors.Count > 0)
            {
                return (false, string.Join("; ", errors));
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse picture update response: {Message}", ex.Message);
            return (true, string.Empty); // Assume success if we can't parse
        }
    }
}
