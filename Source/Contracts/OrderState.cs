using MassTransit;

namespace AspNetCore.API.Contracts;

public record SubmitOrder : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; init; }
    public DateTime OrderDate { get; init; }
}

public sealed class OrderSaga : ISaga, InitiatedBy<SubmitOrder>
{
    public Guid CorrelationId { get; set; }

    public DateTime? SubmitDate { get; set; }
    public DateTime? AcceptDate { get; set; }

    public async Task Consume(ConsumeContext<SubmitOrder> context)
    {
        SubmitDate = context.Message.OrderDate;
        Console.WriteLine("Test");
    }
}