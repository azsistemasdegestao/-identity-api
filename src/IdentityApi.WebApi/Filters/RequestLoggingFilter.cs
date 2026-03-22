using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentityApi.WebApi.Filters;

public sealed class RequestLoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<RequestLoggingFilter> _logger;

    public RequestLoggingFilter(ILogger<RequestLoggingFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sw = Stopwatch.StartNew();
        var user = context.HttpContext.User.Identity?.Name ?? "anonymous";
        var method = context.HttpContext.Request.Method;
        var path = context.HttpContext.Request.Path;

        _logger.LogInformation("Request started: {Method} {Path} | User: {User}", method, path, user);

        var executedContext = await next();

        sw.Stop();

        if (executedContext.Exception is null)
        {
            _logger.LogInformation("Request completed: {Method} {Path} | User: {User} | Duration: {ElapsedMs}ms",
                method, path, user, sw.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogWarning("Request failed: {Method} {Path} | User: {User} | Duration: {ElapsedMs}ms",
                method, path, user, sw.ElapsedMilliseconds);
        }
    }
}
