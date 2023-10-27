using MassTransit;

namespace AspNetCore.API.Contracts;

public sealed class Worker : BackgroundService
{
    private readonly IBus _bus;

    public Worker(IBus bus) => _bus = bus;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _bus.Publish(new GettingStarted { Value = $"The time is {DateTimeOffset.Now}" }, stoppingToken);
            await Task.Delay(1000, stoppingToken);
            await _bus.Publish(new SubmitOrder { OrderDate = DateTime.Now, CorrelationId = Guid.NewGuid() }, stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}