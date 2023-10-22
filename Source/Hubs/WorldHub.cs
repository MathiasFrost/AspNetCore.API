using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.API.Hubs;

public sealed class WorldHub : Hub<IWorldHub>
{
    private readonly IMediator _mediator;

    public WorldHub(IMediator mediator) => _mediator = mediator;

    public async Task SendAll()
    {
        await Clients.All.Worlds(await _mediator.Send(new GetWorldsRequest(), Context.ConnectionAborted), Context.ConnectionAborted);
    }

    public async Task<IEnumerable<World>> Get(string connectionId) => await _mediator.Send(new GetWorldsRequest(), Context.ConnectionAborted);

    public async Task Ping(string text)
    {
        await Clients.All.Pong(text, Context.ConnectionAborted);
    }
}

public interface IWorldHub
{
    public Task Pong(string text, CancellationToken token);
    public Task Worlds(IEnumerable<World> forecasts, CancellationToken token);
}

// {"protocol":"json","version":1}
// {"type":6}
// {"type":1,"target":"Ping","arguments":["text"]}