using System.Runtime.CompilerServices;
using AspNetCore.API.Database;
using AspNetCore.API.Models;
using MediatR;

namespace AspNetCore.API.Handlers;

public sealed class GetWorldsRequest : IRequest<IEnumerable<World>>, IStreamRequest<World> { }

public sealed class GetWorldsHandler : IRequestHandler<GetWorldsRequest, IEnumerable<World>>
{
    private readonly AspNetCoreDb _aspNetCoreDb;

    public GetWorldsHandler(AspNetCoreDb aspNetCoreDb) => _aspNetCoreDb = aspNetCoreDb;

    public async Task<IEnumerable<World>> Handle(GetWorldsRequest request, CancellationToken cancellationToken) =>
        await _aspNetCoreDb
            .Sql("""
                 SELECT *
                 FROM main.World
                 ORDER BY Name
                 """)
            .Query<World>(cancellationToken);
}

public sealed class GetWorldsStreamHandler : IStreamRequestHandler<GetWorldsRequest, World>
{
    private readonly AspNetCoreDb _aspNetCoreDb;

    public GetWorldsStreamHandler(AspNetCoreDb aspNetCoreDb) => _aspNetCoreDb = aspNetCoreDb;

    public async IAsyncEnumerable<World> Handle(GetWorldsRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        long lastId = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var res = await _aspNetCoreDb
                .Sql("""
                     SELECT *
                     FROM main.World
                     WHERE Id > @lastId
                     ORDER BY Id
                     LIMIT 1
                     """)
                .WithParams(new { lastId })
                .QueryFirst<World>(cancellationToken);

            lastId = res.Id;
            yield return res;
            await Task.Delay(1_000, cancellationToken);
        }
    }
}