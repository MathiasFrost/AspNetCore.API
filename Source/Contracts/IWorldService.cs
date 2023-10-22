using System.Runtime.Serialization;
using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using CoreWCF;
using JetBrains.Annotations;
using MediatR;

namespace AspNetCore.API.Contracts;

[PublicAPI, ServiceContract]
public interface IWorldService
{
    [OperationContract]
    IEnumerable<WorldContract> All();
}

[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public sealed class WorldService : IWorldService
{
    private readonly IMediator _mediator;

    public WorldService(IMediator mediator) => _mediator = mediator;

    public IEnumerable<WorldContract> All() =>
        _mediator.Send(new GetWorldsRequest())
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult()
            .Select(static forecast => new WorldContract(forecast));
}

// Use a data contract as illustrated in the sample below to add composite types to service operations.
[PublicAPI, DataContract]
public sealed class WorldContract
{
    public WorldContract(World world)
    {
        Id = world.Id;
        Name = world.Name;
        AvgSurfaceTemp = world.AvgSurfaceTemp;
        Population = world.Population;
        Ecosystem = world.Ecosystem;
        Theme = world.Theme;
    }

    [DataMember] public long Id { get; set; }
    [DataMember] public string Name { get; set; }
    [DataMember] public decimal AvgSurfaceTemp { get; set; }
    [DataMember] public long Population { get; set; }
    [DataMember] public string Ecosystem { get; set; }
    [DataMember] public string Theme { get; set; }
}