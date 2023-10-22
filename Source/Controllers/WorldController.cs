using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]/[action]")]
public sealed class WorldController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorldController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IEnumerable<World>> All(CancellationToken token) => await _mediator.Send(new GetWorldsRequest(), token);

    [HttpGet]
    public Task<IAsyncEnumerable<World>> Stream(CancellationToken token) => Task.FromResult(_mediator.CreateStream(new GetWorldsRequest(), token));

    [HttpPost("{count:int}")]
    public async Task Generate(int count, CancellationToken token) => await _mediator.Publish(new GenerateWorldsRequest { WorldsToGenerate = count }, token);
}