using MassTransit;

namespace AspNetCore.API.Contracts;

public sealed class GettingStartedConsumer : IConsumer<GettingStarted>
{
    private readonly ILogger<GettingStartedConsumer> _logger;
    public static byte test;
    public GettingStartedConsumer(ILogger<GettingStartedConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<GettingStarted> context)
    {
        test++;
        if (test > 10)
        {
            test = 0;
            throw new Exception("Test");
        }

        _logger.LogInformation("Received Text: {Text}", context.Message.Value);
        return Task.CompletedTask;
    }
}