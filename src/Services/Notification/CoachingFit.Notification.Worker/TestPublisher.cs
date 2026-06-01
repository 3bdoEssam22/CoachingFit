using CoachingFit.Contracts.Events;
using MassTransit;

namespace CoachingFit.Notification.Worker;

/// <summary>
/// ⚠️ LESSON 2 SCAFFOLDING ONLY — this whole file gets deleted in Lesson 3.
///
/// It publishes a single test <see cref="CoachActivated"/> a few seconds after
/// startup so you can watch the entire MassTransit pipe end-to-end
/// (Publish → message-type exchange → endpoint exchange → queue → consumer)
/// WITHOUT having to run the Identity service yet.
///
/// In Lesson 3 the real publisher becomes the Identity service (a separate
/// process), which is the actual point of messaging — two decoupled apps.
/// </summary>
public sealed class TestPublisher(IBus bus, ILogger<TestPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the bus a moment to connect to RabbitMQ and declare its topology.
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var test = new CoachActivated(
            UserId: "test-user-123",
            Email: "test.coach@example.com",
            FullName: "Test Coach",
            ActivatedAtUtc: DateTime.UtcNow);

        logger.LogInformation("📤 Publishing test CoachActivated for {Email}...", test.Email);

        // Publish = "announce this event"; MassTransit routes it to every consumer
        // subscribed to CoachActivated (right now, just CoachActivatedConsumer).
        await bus.Publish(test, stoppingToken);
    }
}
