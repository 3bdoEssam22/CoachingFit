using CoachingFit.Contracts.Events;
using MassTransit;

namespace CoachingFit.Notification.Worker.Consumers;

/// <summary>
/// Consumes the <see cref="CoachActivated"/> event.
///
/// MassTransit gives this consumer its own queue (a "receive endpoint"). When a
/// message arrives, MassTransit calls Consume(), and — if it returns without
/// throwing — automatically ACKs the message so RabbitMQ deletes it. If it throws,
/// MassTransit nacks (and, once we add retry in Lesson 4, retries then dead-letters).
///
/// Lesson 2: we only LOG, to prove the pipe works.
/// Lesson 3: this is where we will send the activation email.
/// </summary>
public sealed class CoachActivatedConsumer(ILogger<CoachActivatedConsumer> logger)
    : IConsumer<CoachActivated>
{
    public Task Consume(ConsumeContext<CoachActivated> context)
    {
        var msg = context.Message;

        logger.LogInformation(
            "📥 Received CoachActivated — UserId={UserId}, Email={Email}, FullName={FullName}, ActivatedAtUtc={ActivatedAtUtc:o}",
            msg.UserId, msg.Email, msg.FullName, msg.ActivatedAtUtc);

        return Task.CompletedTask;
    }
}
