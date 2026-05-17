namespace CoachingFit.User.API.Infrastructure.Idempotency
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IdempotentAttribute : Attribute { }
}
