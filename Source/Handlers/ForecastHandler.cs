using System.Runtime.CompilerServices;
using AspNetCore.API.Models;
using MediatR;

namespace AspNetCore.API.Handlers;

public sealed class WeatherForecastRequest : IRequest<IEnumerable<WeatherForecast>>, IStreamRequest<WeatherForecast> { }

public sealed class WeatherForecastHandler : IRequestHandler<WeatherForecastRequest, IEnumerable<WeatherForecast>>
{
    public static readonly string[] Summaries = {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public async Task<IEnumerable<WeatherForecast>> Handle(WeatherForecastRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(1_000, cancellationToken);
        return Enumerable.Range(1, 5)
            .Select(static index => new WeatherForecast {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            });
    }
}

public sealed class WeatherForecastStreamHandler : IStreamRequestHandler<WeatherForecastRequest, WeatherForecast>
{
    public async IAsyncEnumerable<WeatherForecast> Handle(WeatherForecastRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var index = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return new WeatherForecast {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index++)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = WeatherForecastHandler.Summaries[Random.Shared.Next(WeatherForecastHandler.Summaries.Length)]
            };
            await Task.Delay(1_000, cancellationToken);
        }
    }
}