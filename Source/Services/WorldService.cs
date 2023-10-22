using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using Grpc.Core;
using MediatR;

namespace AspNetCore.API.Services;

public sealed class WorldService : Worlds.WorldsBase
{
    private readonly IMediator _mediator;

    public WorldService(IMediator mediator) => _mediator = mediator;

    public override async Task<WorldsResponse> Get(WorldsRequest request, ServerCallContext context)
    {
        IEnumerable<World> res = await _mediator.Send(new GetWorldsRequest(), context.CancellationToken);
        var response = new WorldsResponse();
        response.Worlds.AddRange(res.Select(static world => new WorldResponse {
            Id = world.Id,
            Name = world.Name,
            AvgSurfaceTemp = (double)world.AvgSurfaceTemp,
            Population = world.Population,
            Ecosystem = world.Ecosystem,
            Theme = world.Theme
        }));
        return response;
    }
}