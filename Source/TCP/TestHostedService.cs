using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using MediatR;

namespace AspNetCore.API.TCP;

public sealed class TestHostedService : IHostedService
{
    private readonly ILogger<TestHostedService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CancellationTokenSource _stoppingCts = new();
    private Task? _executingTask;
    private int _executionCount;

    public TestHostedService(ILogger<TestHostedService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background service is starting");

        // Run the message loop in the background
        _executingTask = ExecuteAsync(_stoppingCts.Token);

        // If the task is completed then return it,
        // this will bubble cancellation and failure to the caller.
        if (_executingTask.IsCompleted) return _executingTask;

        // Otherwise, it's running
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background service is stopping");

        // Signal cancellation to the executing method
        _stoppingCts.Cancel();
        _stoppingCts.Dispose();

        // Wait until the task completes or the stop token triggers
        if (_executingTask != null)
            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var recovering = false;
        byte failsInARow = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (recovering)
                {
                    await Task.Delay(10_000, stoppingToken);
                    recovering = false;
                }

                _executionCount++;

                if (_executionCount == 3)
                {
                    _executionCount = 0;
                    throw new Exception("Simulated exception on the 3rd loop.");
                }

                // Simulate some work
                await using (AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope())
                {
                    _logger.LogInformation("Background service is doing work");
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    IEnumerable<WeatherForecast> res = await mediator.Send(new WeatherForecastRequest(), stoppingToken);
                    // Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }));
                    Console.WriteLine($"Forecasts: {res.Count()}");
                }

                failsInARow = 0;
            }
            catch (OperationCanceledException)
            {
                throw; // ASP.NET Core handles this one
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error #{Nr} occurred while executing background service", failsInARow);
                if (failsInARow++ > 3)
                {
                    // Trigger graceful shutdown
                    _stoppingCts.Cancel();
                    break;
                }

                recovering = true;
            }
        }
    }
}