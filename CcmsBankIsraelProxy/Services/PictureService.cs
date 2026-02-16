using CcmsBankIsraelProxy.SoapClients.SapPicture;
using System.ServiceModel;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Updates employee picture in BOI SAP via WCF client generated from comda_pic.wsdl
/// </summary>
public class PictureService(
    ILogger<PictureService> logger,
    IConfiguration configuration) : IPictureService
{
    public async Task<(bool success, string message)> UpdatePictureAsync(string idNum, string cardNum, string pictureBase64)
    {
        if (!configuration.GetValue("Boi:Sap:Enabled", true))
        {
            logger.LogWarning("SAP picture service is disabled in configuration");
            return (false, "SAP picture service is disabled");
        }

        if (string.IsNullOrEmpty(pictureBase64))
        {
            logger.LogWarning("No picture data provided");
            return (false, "No picture data provided");
        }

        string endpoint = configuration.GetValue<string>("Boi:Sap:Endpoint") ?? "";
        string username = configuration.GetValue<string>("Boi:Sap:Username") ?? "";
        string password = configuration.GetValue<string>("Boi:Sap:Password") ?? "";

        logger.LogInformation("Updating SAP picture for ID: {IdNum}, Card: {CardNum}", idNum, cardNum);

        try
        {
            BasicHttpBinding binding = endpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase)
                ? new() { Security = { Mode = BasicHttpSecurityMode.Transport }, MaxReceivedMessageSize = int.MaxValue }
                : new BasicHttpBinding { MaxReceivedMessageSize = int.MaxValue };

            EndpointAddress address = new(endpoint);
            comda_pic_miClient client = new(binding, address);

            if (!string.IsNullOrEmpty(username))
            {
                client.ClientCredentials.UserName.UserName = username;
                client.ClientCredentials.UserName.Password = password;
            }

            poli_pic_req_dt request = new()
            {
                IDNum = idNum,
                CardNum = cardNum,
                Picture = pictureBase64
            };

            poli_pic_miResponse response = await client.poli_pic_miAsync(request);

            poli_pic_res_dtReturnMessage[]? messages = response.poli_pic_res_mt;
            if (messages is { Length: > 0 })
            {
                List<string> errors = messages
                    .Where(m => m.Type is "E" or "Error")
                    .Select(m => m.Message)
                    .ToList();

                if (errors.Count > 0)
                {
                    string errorMessage = string.Join("; ", errors);
                    logger.LogError("SAP picture update failed for ID: {IdNum} - {Message}", idNum, errorMessage);
                    return (false, errorMessage);
                }
            }

            logger.LogInformation("SAP picture update succeeded for ID: {IdNum}", idNum);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in UpdatePictureAsync: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }
}
