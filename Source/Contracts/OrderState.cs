using JetBrains.Annotations;
using MassTransit;

namespace AspNetCore.API.Contracts;

[PublicAPI]
public sealed record SubmitOrder : CorrelatedBy<Guid>
{
    public DateTime OrderDate { get; init; }
    public Guid CorrelationId { get; init; }
}

[PublicAPI]
public sealed class OrderSaga : ISaga, InitiatedBy<SubmitOrder>
{
    public DateTime? SubmitDate { get; set; }
    public DateTime? AcceptDate { get; set; }

    public Task Consume(ConsumeContext<SubmitOrder> context)
    {
        SubmitDate = context.Message.OrderDate;
        Console.WriteLine("Test");
        return Task.CompletedTask;
    }

    public Guid CorrelationId { get; set; }
}