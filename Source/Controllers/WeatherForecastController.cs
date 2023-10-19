using MediatR;
using System.Data;
using AspNetCore.API.Database;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly DatabaseTds _databaseTds;
    private readonly IMediator _mediator;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IMediator mediator, DatabaseTds databaseTds)
    {
        _logger = logger;
        _mediator = mediator;
        _databaseTds = databaseTds;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5)
            .Select(index => new WeatherForecast {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }

    [HttpGet("DateOnly")]
    public DateOnly GetDateOnly([FromQuery] DateOnly? dateOnly)
    {
        Console.WriteLine(dateOnly);
        return dateOnly ?? DateOnly.FromDateTime(DateTime.Today);
    }

    [HttpGet("Test")]
    public async Task<string> Test(CancellationToken token)
    {
        await _mediator.Publish(new MyMessage(), token);
        return await _mediator.Send(new MyMessage(), token);
    }
}

public sealed class MyMessage : INotification, IRequest<string>
{
    public string Res { get; set; } = String.Empty;
}

public sealed class Notify1 : INotificationHandler<MyMessage>
{
    public Task Handle(MyMessage notification, CancellationToken cancellationToken)
    {
        Console.WriteLine("Test 1");
        return Task.CompletedTask;
    }
}

public sealed class Notify2 : INotificationHandler<MyMessage>
{
    public Task Handle(MyMessage notification, CancellationToken cancellationToken)
    {
        Console.WriteLine("Test 2");
        return Task.CompletedTask;
    }
}

public sealed class Handler : IRequestHandler<MyMessage, string>
{
    public Task<string> Handle(MyMessage request, CancellationToken cancellationToken) => Task.FromResult("Test 3");
}