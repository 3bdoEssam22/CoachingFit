using CoachingFit.Notification.Worker;
using CoachingFit.Notification.Worker.Configuration;
using CoachingFit.Notification.Worker.Consumers;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

// Read RabbitMQ connection settings (section "RabbitMq"); fall back to localhost defaults.
var rabbit = builder.Configuration
                 .GetSection(RabbitMqSettings.SectionName)
                 .Get<RabbitMqSettings>()
             ?? new RabbitMqSettings();

builder.Services.AddMassTransit(x =>
{
    // Name queues/exchanges in kebab-case, e.g. CoachActivatedConsumer → "coach-activated".
    // (Without this you'd get PascalCase names; we pick kebab so they're predictable.)
    x.SetKebabCaseEndpointNameFormatter();

    // Register the consumer(s). Each consumer becomes its own queue ("receive endpoint").
    x.AddConsumer<CoachActivatedConsumer>();

    // Use RabbitMQ as the transport.
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbit.Host, rabbit.Port, rabbit.VirtualHost, h =>
        {
            h.Username(rabbit.Username);
            h.Password(rabbit.Password);
        });

        // Auto-declare a queue per consumer and wire all the exchange/queue bindings.
        // This is exactly the topology you built BY HAND in Lesson 1 — now generated.
        cfg.ConfigureEndpoints(context);
    });
});

// ⚠️ LESSON 2 ONLY — publish a test message on startup (Development only). Deleted in Lesson 3.
if (builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<TestPublisher>();

var host = builder.Build();
host.Run();
