namespace AspNetCore.API.TCP;

public sealed class TcpHost : IHostedService
{
    private byte _failureCount;
    private const byte MaxFailures = 3; // maximum failures before breaking the circuit
    private DateTime _circuitResetTime;
    private readonly TimeSpan _circuitOpenTime = TimeSpan.FromMinutes(1); // time to wait before closing the circuit

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => DoWorkAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task RunBackgroundService(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(token);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                break;
            }
            catch (Exception ex)
            {
                // Handle other exceptions
            }
        }
    }

    private async Task DoWorkAsync(CancellationToken token)
    {
        byte loops = 0;
        while (!token.IsCancellationRequested)
        {
            // Check if the circuit is open
            if (_failureCount >= MaxFailures)
            {
                if (DateTime.UtcNow < _circuitResetTime)
                {
                    // Circuit is open; wait for it to close
                        await Task.Delay(_circuitOpenTime, token);

                    continue;
                }

                // Reset the circuit and failure count
                _failureCount = 0;
            }

            try
            {
                // Simulated work
                Console.WriteLine($"PrePing: {_failureCount}");
                await Task.Delay(2_000, token);
                Console.WriteLine($"Ping: {_failureCount}");
                await Task.Delay(2_000, token);

                // Test exception after 10 loops
                if (loops > 10) throw new Exception("Test exception");

                loops++;
            }
            catch (Exception ex)
            {
                // Log the exception (not shown here)
                Console.WriteLine($"Exception: {ex.Message}");

                _failureCount++;
                if (_failureCount >= MaxFailures)
                {
                    // Open the circuit
                    _circuitResetTime = DateTime.UtcNow.Add(_circuitOpenTime);
                    Console.WriteLine("Circuit open");
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop your TCP listener logic here
        Console.WriteLine("Shutting down TCP host");
        return Task.CompletedTask;
    }
}