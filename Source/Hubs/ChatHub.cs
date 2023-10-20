using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.API.Hubs;

public sealed class WeatherForecastHub : Hub<IWeatherForecastHub>
{
    public async Task Ping(string text)
    {
        await Clients.All.Pong(text);
    }
}

public interface IWeatherForecastHub
{
    public Task Pong(string text);
}

// {"protocol":"json","version":1}
// {"type":6}
// {"type":1,"target":"Ping","arguments":["text"]}