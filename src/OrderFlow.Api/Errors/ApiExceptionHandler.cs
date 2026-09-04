using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Exceptions;
using OrderFlow.Domain.Common;

namespace OrderFlow.Api.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<ApiExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            OrderNotFoundException => (
                StatusCodes.Status404NotFound,
                "Order not found"),

            DomainRuleException => (
                StatusCodes.Status409Conflict,
                "Business rule violation"),

            ConcurrencyConflictException => (
                StatusCodes.Status409Conflict,
                "Concurrency conflict"),

            DuplicateExternalOrderException => (
                StatusCodes.Status409Conflict,
                "Duplicate external order"),

            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected server error")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception.");
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with status {StatusCode}.", statusCode);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = statusCode >= 500
                        ? "An unexpected error occurred."
                        : exception.Message
                },
                Exception = exception
            });
    }
}
