namespace FieldOps.Domain.WorkOrders;

public sealed record WorkOrderEvent(
    Guid Id,
    Guid WorkOrderId,
    Guid OrganizationId,
    Guid ActorId,
    Guid CommandId,
    string EventType,
    WorkOrderStatus From,
    WorkOrderStatus To,
    string? Reason,
    DateTimeOffset OccurredAtUtc,
    long Version);
