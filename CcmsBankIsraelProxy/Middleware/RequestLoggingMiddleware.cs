using System.Diagnostics;
using System.Text;

namespace CcmsBankIsraelProxy.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // Log request
        await LogRequest(context, requestId);

        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);

            // Copy the response body back to the original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequest(HttpContext context, string requestId)
    {
        context.Request.EnableBuffering();

        var requestBody = string.Empty;
        
        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);
            
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.ToString();
        var contentType = context.Request.ContentType ?? "none";

        _logger.LogInformation(
            "[{RequestId}] --> {Method} {Path}{QueryString} | IP: {ClientIp} | ContentType: {ContentType}",
            requestId, method, path, queryString, clientIp, contentType);

        if (!string.IsNullOrEmpty(requestBody) && requestBody.Length < 10000)
        {
            _logger.LogDebug("[{RequestId}] Request Body: {RequestBody}", requestId, requestBody);
        }
        else if (!string.IsNullOrEmpty(requestBody))
        {
            _logger.LogDebug("[{RequestId}] Request Body: [truncated - {Length} bytes]", requestId, requestBody.Length);
        }
    }

    private async Task LogResponse(HttpContext context, string requestId, long elapsedMs)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        var statusCode = context.Response.StatusCode;
        var method = context.Request.Method;
        var path = context.Request.Path;

        var logLevel = statusCode >= 500 ? LogLevel.Error 
            : statusCode >= 400 ? LogLevel.Warning 
            : LogLevel.Information;

        _logger.Log(logLevel,
            "[{RequestId}] <-- {StatusCode} {Method} {Path} | Duration: {ElapsedMs}ms",
            requestId, statusCode, method, path, elapsedMs);

        if (!string.IsNullOrEmpty(responseBody) && responseBody.Length < 5000)
        {
            _logger.LogDebug("[{RequestId}] Response Body: {ResponseBody}", requestId, responseBody);
        }
        else if (!string.IsNullOrEmpty(responseBody))
        {
            _logger.LogDebug("[{RequestId}] Response Body: [truncated - {Length} bytes]", requestId, responseBody.Length);
        }
    }
}
