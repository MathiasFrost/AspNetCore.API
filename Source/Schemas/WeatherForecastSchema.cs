using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using GraphQL.Types;
using MediatR;

namespace AspNetCore.API.Schemas;

public sealed class WeatherForecastType : ObjectGraphType<WeatherForecast>
{
    public WeatherForecastType()
    {
        Field(static forecast => forecast.Date);
        Field(static forecast => forecast.TemperatureC);
        Field(static forecast => forecast.TemperatureF);
        Field(static forecast => forecast.Summary);
    }
}

public sealed class WeatherForecastQuery : ObjectGraphType
{
    public WeatherForecastQuery(IMediator mediator)
    {
        Field<ListGraphType<WeatherForecastType>>("weatherforecasts")
            .ResolveAsync(async context => await mediator.Send(new WeatherForecastRequest(), context.CancellationToken));
    }
}

public sealed class WeatherForecastSchema : Schema
{
    public WeatherForecastSchema(IMediator mediator) => Query = new WeatherForecastQuery(mediator);
}