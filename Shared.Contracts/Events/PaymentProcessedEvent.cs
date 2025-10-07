namespace Shared.Contracts.Events;

public class PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    public bool IsSuccess { get; init; }
    public string? FailureReason { get; init; }
}
