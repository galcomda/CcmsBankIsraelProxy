using CcmsBankIsraelProxy.Models;
using System.Text;
using System.Text.Json;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Updates employee data in SRHR via REST POST with Windows Authentication.
/// Based on BOI_WS.UpdateSRHR() pattern.
/// </summary>
public class SrhrService(
    ILogger<SrhrService> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ISrhrService
{
    public async Task<(bool success, string message)> UpdatePolimilAsync(PolimilRequest request)
    {
        if (!configuration.GetValue("Boi:Srhr:Enabled", true))
        {
            logger.LogWarning("SRHR service is disabled in configuration");
            return (false, "SRHR service is disabled");
        }

        string endpoint = configuration.GetValue<string>("Boi:Srhr:Endpoint") ?? "";
        if (string.IsNullOrEmpty(endpoint))
        {
            logger.LogError("SRHR endpoint is not configured");
            return (false, "SRHR endpoint is not configured");
        }

        logger.LogInformation("Updating SRHR for ID: {Id}, Employee: {EmployeeId}, Card: {CardId}",
            request.Id, request.EmployeeId, request.CardId);

        try
        {
            HttpClient client = httpClientFactory.CreateClient("Srhr");

            string json = JsonSerializer.Serialize(request);
            using StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(endpoint, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("SRHR update failed with status {StatusCode}: {Body}", response.StatusCode, responseBody);
                return (false, $"SRHR returned error: {response.StatusCode}");
            }

            logger.LogInformation("SRHR update succeeded for ID: {Id}", request.Id);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in UpdatePolimilAsync: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }
}
