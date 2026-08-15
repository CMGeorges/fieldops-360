using FieldOps.Domain.WorkOrders;

namespace FieldOps.Application.WorkOrders;

public sealed record CommandContext(Guid OrganizationId, Guid ActorId, Guid CommandId);

public sealed record CreateWorkOrder(
    string Title,
    string Description,
    WorkOrderPriority Priority);

public sealed record WorkOrderView(
    Guid Id,
    Guid OrganizationId,
    string Title,
    string Description,
    string Priority,
    string Status,
    Guid? ContractorId,
    DateTimeOffset? OfferExpiresAtUtc,
    DateTimeOffset? ScheduledForUtc,
    DateTimeOffset? SlaDueAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version,
    IReadOnlyList<WorkOrderEventView> Timeline);

public sealed record WorkOrderEventView(
    string Type,
    string From,
    string To,
    string? Reason,
    Guid ActorId,
    DateTimeOffset OccurredAtUtc,
    long Version);

internal static class WorkOrderMappings
{
    public static WorkOrderView ToView(this WorkOrder order) => new(
        order.Id,
        order.OrganizationId,
        order.Title,
        order.Description,
        order.Priority.ToString(),
        order.Status.ToString(),
        order.ContractorId,
        order.OfferExpiresAtUtc,
        order.ScheduledForUtc,
        order.SlaDueAtUtc,
        order.CreatedAtUtc,
        order.UpdatedAtUtc,
        order.Version,
        order.Events.Select(item => new WorkOrderEventView(
            item.EventType,
            item.From.ToString(),
            item.To.ToString(),
            item.Reason,
            item.ActorId,
            item.OccurredAtUtc,
            item.Version)).ToArray());
}

