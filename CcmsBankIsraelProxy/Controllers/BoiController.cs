using CcmsBankIsraelProxy.Models;
using CcmsBankIsraelProxy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CcmsBankIsraelProxy.Controllers;

[ApiController]
[Route("[controller]")]
public class BoiController(
    ILogger<BoiController> logger,
    IBoiHandler boiHandler,
    IOptions<BoiCardFields> cardFieldsOptions) : ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return Ok("BOI Proxy is running.");
    }

    /// <summary>
    /// Process CCMS callback: updates SAP (picture) and SRHR.
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> Process(CallbackData callbackData)
    {
        logger.LogInformation("BOI Process - Issuer: {Issuer}, Operation: {Operation}", callbackData.Issuer, callbackData.Operation);
        logger.LogDebug("Raw CardData: {CardData}", callbackData.CardData);

        BoiCardFields cardFields = cardFieldsOptions.Value;
        Operations operation = (Operations)callbackData.Operation;

        Dictionary<string, object>? cardData;
        try
        {
            cardData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(callbackData.CardData);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to deserialize card data: {CardData}", callbackData.CardData);
            return Ok(new CallbackResponse { IsValid = false, Message = "נכשל בקריאת נתוני כרטיס" });
        }

        if (cardData == null)
        {
            logger.LogError("Card data is null after deserialization: {CardData}", callbackData.CardData);
            return Ok(new CallbackResponse { IsValid = false, Message = "נכשל בקריאת נתוני כרטיס" });
        }

        object? idNumber = cardData.TryGetValue(cardFields.IdNumber, out object? idNum) ? idNum : null;
        object? employeeNumber = cardData.TryGetValue(cardFields.EmployeeNumber, out object? empNum) ? empNum : null;
        logger.LogInformation("Processing {Operation} - ID: {IdNumber}, Employee: {EmployeeNumber}, Issuer: {Issuer}",
            operation, idNumber, employeeNumber, callbackData.Issuer);

        (bool success, string message) = await boiHandler.HandleBoiCallback(cardData, operation);

        if (!success)
        {
            logger.LogError("BOI process failed - Operation: {Operation}, ID: {IdNumber}, Error: {Message}", operation, idNumber, message);
            return Ok(new CallbackResponse { IsValid = false, Message = message });
        }

        logger.LogInformation("BOI process completed - Operation: {Operation}, ID: {IdNumber}", operation, idNumber);
        return Ok(new CallbackResponse { IsValid = true });
    }
}
