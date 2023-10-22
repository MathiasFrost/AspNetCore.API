using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using GraphQL.Types;
using JetBrains.Annotations;
using MediatR;

namespace AspNetCore.API.Schemas;

public sealed class WorldType : ObjectGraphType<World>
{
    public WorldType()
    {
        Field(static world => world.Id);
        Field(static world => world.Name);
        Field(static world => world.AvgSurfaceTemp);
        Field(static world => world.Population);
        Field(static world => world.Ecosystem);
        Field(static world => world.Theme);
    }
}

[UsedImplicitly]
public sealed class WorldQuery : ObjectGraphType
{
    public WorldQuery()
    {
        Field<ListGraphType<WorldType>>("worlds")
            .ResolveAsync(static async context =>
                await context.RequestServices!.GetRequiredService<ISender>().Send(new GetWorldsRequest(), context.CancellationToken));
    }
}

[UsedImplicitly]
public sealed class WorldSchema : Schema
{
    public WorldSchema() => Query = new WorldQuery();
}