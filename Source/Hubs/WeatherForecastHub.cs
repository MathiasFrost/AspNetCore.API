using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.API.Hubs;

public sealed class WeatherForecastHub : Hub<IWeatherForecastHub>
{
    private readonly IMediator _mediator;

    public WeatherForecastHub(IMediator mediator) => _mediator = mediator;

    public async Task Query()
    {
        await Clients.All.Forecasts(await _mediator.Send(new WeatherForecastRequest(), Context.ConnectionAborted), Context.ConnectionAborted);
    }

    public async Task<IEnumerable<WeatherForecast>> Get(string connectionId) => await _mediator.Send(new WeatherForecastRequest(), Context.ConnectionAborted);

    public async Task Ping(string text)
    {
        await Clients.All.Pong(text, Context.ConnectionAborted);
    }
}

public interface IWeatherForecastHub
{
    public Task Pong(string text, CancellationToken token);
    public Task Forecasts(IEnumerable<WeatherForecast> forecasts, CancellationToken token);
}

// {"protocol":"json","version":1}
// {"type":6}
// {"type":1,"target":"Ping","arguments":["text"]}