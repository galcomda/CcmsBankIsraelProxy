using CcmsBankIsraelProxy.Models;
using CcmsBankIsraelProxy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CcmsBankIsraelProxy.Controllers;

[ApiController]
[Route("[controller]")]
public class BoiController(
    ILogger<BoiController> logger,
    IBoiHandler boiHandler,
    IOptions<BoiCardFields> cardFieldsOptions) : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        logger.LogDebug("Health check request received");
        return Ok(new { status = "healthy", service = "BOI Proxy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Process CCMS callback - main entry point for card operations
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> Process(CallbackData callbackData)
    {
        logger.LogInformation("========== BOI Process Request Received ==========");
        logger.LogInformation("Issuer: {Issuer}, Operation: {Operation}", callbackData.Issuer, callbackData.Operation);
        logger.LogDebug("Raw CardData: {CardData}", callbackData.CardData);

        var cardFields = cardFieldsOptions.Value;
        Operations operation = (Operations)callbackData.Operation;

        logger.LogDebug("Processing operation: {Operation} ({OperationId})", operation, (int)operation);

        Dictionary<string, object>? cardData;

        try
        {
            cardData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(callbackData.CardData);
            logger.LogDebug("Successfully deserialized card data with {Count} fields", cardData?.Count ?? 0);

            if (cardData != null)
            {
                logger.LogTrace("Card data fields: {Fields}", string.Join(", ", cardData.Keys));
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "FAILED to deserialize card data: {CardData}", callbackData.CardData);

            return Ok(new CallbackResponse()
            {
                IsValid = false,
                Message = "נכשל בקריאת נתוני כרטיס"
            });
        }

        if (cardData == null)
        {
            logger.LogError("Card data is NULL after deserialization: {CardData}", callbackData.CardData);

            return Ok(new CallbackResponse()
            {
                IsValid = false,
                Message = "נכשל בקריאת נתוני כרטיס"
            });
        }

        var idNumber = cardData.TryGetValue(cardFields.IdNumber, out var idNum) ? idNum : null;
        var employeeNumber = cardData.TryGetValue(cardFields.EmployeeNumber, out var empNum) ? empNum : null;

        logger.LogInformation("Processing {Operation} - ID: {IdNumber}, Employee: {EmployeeNumber}, Issuer: {Issuer}",
            operation, idNumber, employeeNumber, callbackData.Issuer);

        (bool success, string message) = await boiHandler.HandleBoiCallback(cardData, operation);

        if (!success)
        {
            logger.LogError("BOI process FAILED - Operation: {Operation}, ID: {IdNumber}, Error: {Message}",
                operation, idNumber, message);
            return Ok(new CallbackResponse()
            {
                IsValid = false,
                Message = message
            });
        }

        logger.LogInformation("BOI process COMPLETED successfully - Operation: {Operation}, ID: {IdNumber}",
            operation, idNumber);

        return Ok(new CallbackResponse()
        {
            IsValid = true
        });
    }

    /// <summary>
    /// Get employee data from BOI SAP
    /// </summary>
    [HttpGet("employee/{idNumber}")]
    public async Task<IActionResult> GetEmployee(string idNumber)
    {
        logger.LogInformation("GetEmployee request - ID: {IdNumber}", idNumber);

        var (success, employee, message) = await boiHandler.GetEmployee(idNumber);

        if (!success)
        {
            logger.LogError("GetEmployee FAILED - ID: {IdNumber}, Error: {Message}", idNumber, message);
            return NotFound(new { success = false, message });
        }

        return Ok(new { success = true, employee });
    }

    /// <summary>
    /// Update employee picture in BOI SAP
    /// </summary>
    [HttpPost("picture")]
    public async Task<IActionResult> UpdatePicture([FromBody] PictureUpdateRequest request)
    {
        logger.LogInformation("UpdatePicture request - ID: {IdNumber}, CardNum: {CardNum}",
            request.IdNum, request.CardNum);

        var (success, message) = await boiHandler.UpdatePicture(request.IdNum, request.CardNum, request.Picture ?? "");

        if (!success)
        {
            logger.LogError("UpdatePicture FAILED - ID: {IdNumber}, Error: {Message}", request.IdNum, message);
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Send SMS via BOI SMS service
    /// </summary>
    [HttpPost("sms")]
    public async Task<IActionResult> SendSms([FromBody] SmsRequest request)
    {
        logger.LogInformation("SendSms request - To: {ToNumbers}", request.ToNumbers);

        var (success, message) = await boiHandler.SendSms(request.ToNumbers, request.Message);

        if (!success)
        {
            logger.LogError("SendSms FAILED - To: {ToNumbers}, Error: {Message}", request.ToNumbers, message);
            return BadRequest(new { success = false, message });
        }

        return Ok(new { success = true, result = message });
    }
}
