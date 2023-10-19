using System.Runtime.Serialization;
using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using CoreWCF;
using MediatR;

namespace AspNetCore.API.Contracts;

[ServiceContract]
public interface IWeatherForecastService
{
    [OperationContract]
    IEnumerable<CompositeType> Get();
}

[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public sealed class WeatherForecastService : IWeatherForecastService
{
    private readonly IMediator _mediator;

    public WeatherForecastService(IMediator mediator) => _mediator = mediator;

    public IEnumerable<CompositeType> Get() =>
        _mediator.Send(new WeatherForecastRequest()).ConfigureAwait(false).GetAwaiter().GetResult().Select(static forecast => new CompositeType(forecast));
}

// Use a data contract as illustrated in the sample below to add composite types to service operations.
[DataContract]
public sealed class CompositeType
{
    public CompositeType(WeatherForecast weatherForecast)
    {
        Date = weatherForecast.Date.ToString("O");
        TemperatureC = weatherForecast.TemperatureC;
        TemperatureF = weatherForecast.TemperatureF;
        Summary = weatherForecast.Summary;
    }

    [DataMember] public string Date { get; set; }

    [DataMember] public int TemperatureC { get; set; }

    [DataMember] public int TemperatureF { get; set; }

    [DataMember] public string? Summary { get; set; }
}