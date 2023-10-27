using MassTransit;
using MassTransit.DapperIntegration.Saga;
using MassTransit.Saga;

namespace AspNetCore.API.Contracts;

public class DapperSqlLite<TSaga> : DapperSagaRepositoryContextFactory<TSaga> where TSaga : class, ISaga
{
    public DapperSqlLite(DapperOptions<TSaga> options, ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory) : base(options, factory) { }
}