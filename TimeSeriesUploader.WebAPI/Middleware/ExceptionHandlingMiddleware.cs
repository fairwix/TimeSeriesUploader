using System.Net;
using System.Text.Json;
using FluentValidation;

namespace TimeSeriesUploader.WebAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env; 

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        int statusCode;
        string title;
        string detail;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                title = "Validation failed";
                detail = _env.IsDevelopment() 
                    ? validationException.Message 
                    : "One or more validation errors occurred.";
                _logger.LogWarning(validationException, "Validation error for {Path}: {Message}", 
                    context.Request.Path, validationException.Message);
                break;

            case OperationCanceledException:
                statusCode = 499; 
                title = "Request cancelled";
                detail = "The request was cancelled by the client.";
                _logger.LogInformation("Request cancelled for {Path}", context.Request.Path);
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                title = "An unexpected error occurred";
                detail = _env.IsDevelopment() 
                    ? exception.ToString()
                    : "Internal server error. Please try again later.";
                _logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
                break;
        }

        context.Response.StatusCode = statusCode;

        var problemDetails = new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}