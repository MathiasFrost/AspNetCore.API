using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]/[action]")]
public sealed class WeatherForecastController : ControllerBase
{
    private readonly IMediator _mediator;

    public WeatherForecastController(IMediator mediator) => _mediator = mediator;

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get(CancellationToken token) => await _mediator.Send(new WeatherForecastRequest(), token);

    [HttpGet]
    public Task<IAsyncEnumerable<WeatherForecast>> Stream(CancellationToken token) =>
        Task.FromResult(_mediator.CreateStream(new WeatherForecastRequest(), token));
}