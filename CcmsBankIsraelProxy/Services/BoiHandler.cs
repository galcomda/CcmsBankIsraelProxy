using CcmsBankIsraelProxy.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Main handler for BOI operations - orchestrates SAP, Picture, and SMS services
/// </summary>
public class BoiHandler : IBoiHandler
{
    private readonly ILogger<BoiHandler> _logger;
    private readonly ISapEmployeeService _sapService;
    private readonly IPictureService _pictureService;
    private readonly ISmsService _smsService;
    private readonly BoiCardFields _cardFields;
    private readonly IConfiguration _configuration;

    public BoiHandler(
        ILogger<BoiHandler> logger,
        ISapEmployeeService sapService,
        IPictureService pictureService,
        ISmsService smsService,
        IOptions<BoiCardFields> cardFieldsOptions,
        IConfiguration configuration)
    {
        _logger = logger;
        _sapService = sapService;
        _pictureService = pictureService;
        _smsService = smsService;
        _cardFields = cardFieldsOptions.Value;
        _configuration = configuration;
    }

    public async Task<(bool success, string message)> HandleBoiCallback(Dictionary<string, object> cardData, Operations operation)
    {
        _logger.LogInformation("========== BOI Callback Handler ==========");
        _logger.LogInformation("Operation: {Operation}, CardData fields: {Fields}",
            operation, string.Join(", ", cardData.Keys));
        _logger.LogDebug("CardData: {CardData}", JsonSerializer.Serialize(cardData));

        try
        {
            // Extract key fields from card data
            var idNumber = GetCardValue(cardData, _cardFields.IdNumber);
            var cardNumber = GetCardValue(cardData, _cardFields.CardNumber);
            var employeeNumber = GetCardValue(cardData, _cardFields.EmployeeNumber);

            _logger.LogInformation("Processing - ID: {IdNumber}, Card: {CardNumber}, Employee: {EmployeeNumber}",
                idNumber, cardNumber, employeeNumber);

            // Based on operation, perform different actions
            switch (operation)
            {
                case Operations.CREATE:
                case Operations.UPDATE:
                case Operations.PRINT:
                    return await HandleCreateOrUpdate(cardData, idNumber, cardNumber);

                case Operations.DELETE:
                case Operations.REVOKE:
                    _logger.LogInformation("Operation {Operation} - No BOI action required", operation);
                    return (true, string.Empty);

                default:
                    _logger.LogInformation("Operation {Operation} - No specific BOI handler", operation);
                    return (true, string.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in HandleBoiCallback: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }

    private async Task<(bool success, string message)> HandleCreateOrUpdate(
        Dictionary<string, object> cardData, 
        string idNumber, 
        string cardNumber)
    {
        _logger.LogInformation("HandleCreateOrUpdate - ID: {IdNumber}, CardNumber: {CardNumber}", idNumber, cardNumber);

        // Update picture if provided
        var pictureBase64 = GetCardValue(cardData, _cardFields.PhotoBase64);
        if (string.IsNullOrEmpty(pictureBase64))
        {
            pictureBase64 = GetCardValue(cardData, _cardFields.Photo);
        }

        if (!string.IsNullOrEmpty(pictureBase64) && !string.IsNullOrEmpty(idNumber))
        {
            _logger.LogInformation("Picture data found, updating in BOI SAP...");
            
            var (picSuccess, picMessage) = await UpdatePicture(idNumber, cardNumber, pictureBase64);
            
            if (!picSuccess)
            {
                _logger.LogWarning("Picture update failed (non-blocking): {Message}", picMessage);
                // Picture update failure is not blocking - continue with other operations
            }
        }
        else
        {
            _logger.LogDebug("No picture data provided, skipping picture update");
        }

        // Send SMS notification if configured
        var sendSmsOnUpdate = _configuration.GetValue("Boi:Sms:SendOnCardUpdate", false);
        if (sendSmsOnUpdate)
        {
            var phoneNumber = GetCardValue(cardData, _cardFields.PhoneNumber);
            var smsTemplate = _configuration.GetValue<string>("Boi:Sms:CardUpdateMessage") 
                ?? "Your card has been updated successfully.";

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                _logger.LogInformation("Sending SMS notification to: {Phone}", phoneNumber);
                var (smsSuccess, smsMessage) = await SendSms(phoneNumber, smsTemplate);
                
                if (!smsSuccess)
                {
                    _logger.LogWarning("SMS send failed (non-blocking): {Message}", smsMessage);
                }
            }
        }

        _logger.LogInformation("HandleCreateOrUpdate completed successfully");
        return (true, string.Empty);
    }

    public async Task<(bool success, EmployeeData? employee, string message)> GetEmployee(string idNumber)
    {
        _logger.LogInformation("GetEmployee - ID: {IdNumber}", idNumber);
        return await _sapService.GetEmployeeByIdAsync(idNumber);
    }

    public async Task<(bool success, string message)> UpdatePicture(string idNumber, string cardNumber, string pictureBase64)
    {
        _logger.LogInformation("UpdatePicture - ID: {IdNumber}, CardNumber: {CardNumber}", idNumber, cardNumber);
        
        var request = new PictureUpdateRequest
        {
            IdNum = idNumber,
            CardNum = cardNumber,
            Picture = pictureBase64
        };

        return await _pictureService.UpdatePictureAsync(request);
    }

    public async Task<(bool success, string message)> SendSms(string phoneNumber, string message)
    {
        _logger.LogInformation("SendSms - Phone: {Phone}", phoneNumber);
        return await _smsService.SendSmsAsync(phoneNumber, message);
    }

    private static string GetCardValue(Dictionary<string, object> cardData, string key)
    {
        return cardData.TryGetValue(key, out var value) ? $"{value}" : string.Empty;
    }
}
