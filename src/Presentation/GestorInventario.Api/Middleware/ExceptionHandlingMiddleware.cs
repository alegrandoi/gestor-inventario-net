using System.Net;
using System.Text.Json;
using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Title = "An unexpected error occurred",
            Detail = exception.Message,
            Status = (int)statusCode
        };

        switch (exception)
        {
            case NotFoundException notFound:
                statusCode = HttpStatusCode.NotFound;
                problemDetails.Title = "Resource not found";
                problemDetails.Detail = notFound.Message;
                break;
            case FluentValidation.ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Title = "Validation failed";
                problemDetails.Detail = "One or more validation errors occurred.";
                problemDetails.Extensions["errors"] = validationException.Errors
                    .Select(error => new { error.PropertyName, error.ErrorMessage })
                    .ToArray();
                break;
            case ApplicationValidationException domainValidationException:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Title = "Validation failed";
                problemDetails.Detail = domainValidationException.Message;
                problemDetails.Extensions["errors"] = domainValidationException.Errors
                    .Select(error => new { error.PropertyName, error.ErrorMessage })
                    .ToArray();
                break;
        }

        logger.LogError(exception, "Unhandled exception while processing request");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        problemDetails.Status = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails)).ConfigureAwait(false);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
