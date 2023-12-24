using MassTransit;

namespace AspNetCore.API.Contracts;

public sealed class GettingStartedConsumer : IConsumer<GettingStarted>
{
    private static byte _test;
    private readonly ILogger<GettingStartedConsumer> _logger;
    public GettingStartedConsumer(ILogger<GettingStartedConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<GettingStarted> context)
    {
        _test++;
        if (_test > 10)
        {
            _test = 0;
            throw new Exception("Test");
        }

        _logger.LogInformation("Received Text: {Text}", context.Message.Value);
        return Task.CompletedTask;
    }
}