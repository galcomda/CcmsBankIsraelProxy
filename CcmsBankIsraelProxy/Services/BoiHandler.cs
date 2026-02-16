using CcmsBankIsraelProxy.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Orchestrates SAP picture update and SRHR update for BOI card operations
/// </summary>
public class BoiHandler(
    ILogger<BoiHandler> logger,
    IPictureService pictureService,
    ISrhrService srhrService,
    IOptions<BoiCardFields> cardFieldsOptions,
    IConfiguration configuration) : IBoiHandler
{
    private readonly BoiCardFields _cardFields = cardFieldsOptions.Value;

    public async Task<(bool success, string message)> HandleBoiCallback(Dictionary<string, object> cardData, Operations operation)
    {
        logger.LogInformation("HandleBoiCallback - Operation: {Operation}, Fields: {Fields}",
            operation, string.Join(", ", cardData.Keys));
        logger.LogDebug("CardData: {CardData}", JsonSerializer.Serialize(cardData));

        try
        {
            string idNumber = GetCardValue(cardData, _cardFields.IdNumber);
            string cardNumber = GetCardValue(cardData, _cardFields.CardNumber);
            string employeeNumber = GetCardValue(cardData, _cardFields.EmployeeNumber);

            logger.LogInformation("Processing - ID: {IdNumber}, Card: {CardNumber}, Employee: {EmployeeNumber}",
                idNumber, cardNumber, employeeNumber);
                
            return await HandleCreateOrUpdate(cardData, idNumber, cardNumber, employeeNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in HandleBoiCallback: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }

    private async Task<(bool success, string message)> HandleCreateOrUpdate(
        Dictionary<string, object> cardData,
        string idNumber,
        string cardNumber,
        string employeeNumber)
    {
        logger.LogInformation("HandleCreateOrUpdate - ID: {IdNumber}, Card: {CardNumber}", idNumber, cardNumber);

        // 1. Update SAP picture
        string pictureBase64 = GetCardValue(cardData, _cardFields.PhotoBase64);
        if (string.IsNullOrEmpty(pictureBase64))
        {
            pictureBase64 = GetCardValue(cardData, _cardFields.Photo);
        }

        if (!string.IsNullOrEmpty(pictureBase64) && !string.IsNullOrEmpty(idNumber))
        {
            logger.LogInformation("Updating SAP picture...");
            (bool picSuccess, string picMessage) = await pictureService.UpdatePictureAsync(idNumber, cardNumber, pictureBase64);

            if (!picSuccess)
            {
                logger.LogWarning("SAP picture update failed (non-blocking): {Message}", picMessage);
            }
        }
        else
        {
            logger.LogDebug("No picture data provided, skipping SAP picture update");
        }

        // 2. Update SRHR
        string factory = configuration.GetValue<string>("Boi:Srhr:Factory") ?? "0000";

        PolimilRequest polimilRequest = new()
        {
            Id = idNumber,
            CardId = cardNumber,
            EmployeeId = employeeNumber,
            EmployeeType = GetCardValue(cardData, _cardFields.EmployeeType),
            Factory = factory,
            FirstName = GetCardValue(cardData, _cardFields.FirstName),
            LastName = GetCardValue(cardData, _cardFields.LastName),
            Image = string.IsNullOrEmpty(pictureBase64) ? null : Convert.FromBase64String(pictureBase64)
        };

        logger.LogInformation("Updating SRHR...");
        (bool srhrSuccess, string srhrMessage) = await srhrService.UpdatePolimilAsync(polimilRequest);

        if (!srhrSuccess)
        {
            logger.LogError("SRHR update failed: {Message}", srhrMessage);
            return (false, srhrMessage);
        }

        logger.LogInformation("HandleCreateOrUpdate completed successfully");
        return (true, string.Empty);
    }

    private static string GetCardValue(Dictionary<string, object> cardData, string key)
    {
        return cardData.TryGetValue(key, out object? value) ? $"{value}" : string.Empty;
    }
}
