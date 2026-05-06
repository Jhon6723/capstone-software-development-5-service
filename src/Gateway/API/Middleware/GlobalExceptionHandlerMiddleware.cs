using System.Net;
using System.Text.Json;

namespace API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var errorResponse = new ErrorResponse
        {
            Source = "Gateway"
        };

        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = "Unauthorized access";
                errorResponse.Details = exception.Message;
                break;
            
            case ArgumentException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Invalid request";
                errorResponse.Details = exception.Message;
                break;
            
            case TimeoutException:
                context.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
                errorResponse.StatusCode = (int)HttpStatusCode.GatewayTimeout;
                errorResponse.Message = "Request timeout";
                errorResponse.Details = "The downstream service took too long to respond";
                break;
            
            case HttpRequestException:
                context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                errorResponse.StatusCode = (int)HttpStatusCode.BadGateway;
                errorResponse.Message = "Service unavailable";
                errorResponse.Details = "Unable to reach downstream service";
                break;
            
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "An internal server error occurred";
                errorResponse.Details = "Please contact support if the problem persists";
                break;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(errorResponse, options);
        await context.Response.WriteAsync(json);
    }
}
