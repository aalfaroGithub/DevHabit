using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace DevHabit.Api.Services.Idempotency;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentRequestAttribute : Attribute, IAsyncActionFilter
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(60);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(
                IdempotencyKeyHeader,
                out StringValues idempotenceKeyValue) ||
            !Guid.TryParse(idempotenceKeyValue, out Guid idempotenceKey))
        {
            ProblemDetailsFactory problemDetailsFactory = context.HttpContext.RequestServices
                .GetRequiredService<ProblemDetailsFactory>();
            ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(
                context.HttpContext,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: $"Invalid or missing {IdempotencyKeyHeader} header"
            );
            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }

        IMemoryCache cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
        string cacheKey = $"idempotence:{idempotenceKey}";

        int? statusCode = cache.Get<int?>(cacheKey);
        if (statusCode is not null)
        {
            var result = new StatusCodeResult(statusCode.Value);
            context.Result = result;

            return;
        }

        // If we reach this point, it means the request is being processed for the first time. We execute the action and cache the result.
        ActionExecutedContext executedContext = await next();

        // After the action has been executed, we check the result and cache the status code for future requests with the same idempotency key.
        if (executedContext.Result is ObjectResult objectResult)
        {
            // Also can be cached the location header and the response body.
            // Caching an object instead of the objectResult.StatusCode
            cache.Set(cacheKey, objectResult.StatusCode, DefaultCacheDuration);
        }
    }
}
