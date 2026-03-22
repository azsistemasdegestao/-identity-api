using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentityApi.WebApi.Filters;

public sealed class GlobalExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception occurred. TraceId: {TraceId}", context.HttpContext.TraceIdentifier);

        var result = new ObjectResult(new
        {
            Success = false,
            Message = "An unexpected error occurred. Please try again later.",
            TraceId = context.HttpContext.TraceIdentifier
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        context.Result = result;
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
