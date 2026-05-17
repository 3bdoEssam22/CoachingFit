namespace CoachingFit.Identity.API.Infrastructure.Idempotency
{
    public sealed record CachedActionResult(
        int StatusCode,
        string BodyJson,
        string BodyHash,
        string ContentType);
}
