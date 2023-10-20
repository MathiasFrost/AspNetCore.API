using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using Grpc.Core;
using MediatR;

namespace AspNetCore.API.Services;

public sealed class WeatherForecastService : WeatherForecasts.WeatherForecastsBase
{
    private readonly IMediator _mediator;

    public WeatherForecastService(IMediator mediator) => _mediator = mediator;

    public override async Task<WeatherForecastsResponse> Get(WeatherForecastsRequest request, ServerCallContext context)
    {
        IEnumerable<WeatherForecast> res = await _mediator.Send(new WeatherForecastRequest(), context.CancellationToken);
        var response = new WeatherForecastsResponse();
        response.Forecasts.AddRange(res.Select(static forecast => new WeatherForecastResponse {
            Date = forecast.Date.ToString("O"),
            TemperatureC = forecast.TemperatureC,
            TemperatureF = forecast.TemperatureF,
            Summary = forecast.Summary
        }));
        return response;
    }
}