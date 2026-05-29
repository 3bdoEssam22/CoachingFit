using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Hybrid;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CoachingFit.User.API.Infrastructure.Idempotency
{
    public sealed class IdempotencyKeyFilter(HybridCache _cache) : IAsyncActionFilter
    {
        private const int MaxKeyLength = 128;

        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            var hasAttribute = ctx.ActionDescriptor.EndpointMetadata
                .OfType<IdempotentAttribute>()
                .Any();

            if (!hasAttribute)
            {
                await next();
                return;
            }

            if (!ctx.HttpContext.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues)
                || keyValues.Count != 1
                || string.IsNullOrWhiteSpace(keyValues[0])
                || keyValues[0]!.Length > MaxKeyLength
                || !IsAscii(keyValues[0]!))
            {
                ctx.Result = new BadRequestObjectResult(new { Message = "Idempotency-Key header is required for this endpoint." });
                return;
            }

            var idempotencyKey = keyValues[0]!;
            var bodyHash = ComputeBodyHash(ctx.ActionArguments);

            var controller = ctx.RouteData.Values["controller"];
            var action = ctx.RouteData.Values["action"];
            var cacheKey = $"idem:{controller}:{action}:{idempotencyKey}";

            bool factoryRan = false;
            CachedActionResult? freshResult = null;
            var cached = await _cache.GetOrCreateAsync<CachedActionResult?>(
                cacheKey,
                async ct =>
                {
                    var executed = await next();
                    factoryRan = true;
                    freshResult = CaptureResult(executed, bodyHash);
                    return freshResult;
                },
                cancellationToken: ctx.HttpContext.RequestAborted);

            // factoryRan is true when the cache miss path ran (result already set by next())
            if (factoryRan)
                return;

            // Cache hit — replay
            if (cached is null)
            {
                // 5xx was not cached; re-execute fresh
                await next();
                return;
            }

            if (cached.BodyHash != bodyHash)
            {
                ctx.Result = new UnprocessableEntityObjectResult(new { Message = "Idempotency-Key was reused with a different request body." });
                return;
            }

            ctx.Result = new ContentResult
            {
                StatusCode = cached.StatusCode,
                Content = cached.BodyJson,
                ContentType = cached.ContentType
            };
        }

        private static CachedActionResult? CaptureResult(ActionExecutedContext executed, string bodyHash)
        {
            if (executed.Result is not ObjectResult objectResult || objectResult.Value is null)
                return null;

            var statusCode = objectResult.StatusCode ?? 200;
            if (statusCode >= 500)
                return null; // don't cache 5xx

            var bodyJson = JsonSerializer.Serialize(objectResult.Value);
            return new CachedActionResult(statusCode, bodyJson, bodyHash, "application/json; charset=utf-8");
        }

        private static string ComputeBodyHash(IDictionary<string, object?> arguments)
        {
            // Hash only JSON-serializable body fields. Skip framework-injected parameters
            // (CancellationToken contains an IntPtr WaitHandle; form types are non-serializable).
            var hashable = arguments
                .Where(kv => kv.Value is not CancellationToken
                          && kv.Value is not Microsoft.AspNetCore.Http.IFormFile
                          && kv.Value is not IEnumerable<Microsoft.AspNetCore.Http.IFormFile>
                          && kv.Value is not Microsoft.AspNetCore.Http.IFormCollection)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var json = JsonSerializer.Serialize(hashable, new JsonSerializerOptions { WriteIndented = false });
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(bytes);
        }

        private static bool IsAscii(string value)
        {
            foreach (var c in value)
                if (c > 127) return false;
            return true;
        }
    }
}
