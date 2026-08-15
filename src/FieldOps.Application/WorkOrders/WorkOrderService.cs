using FieldOps.Domain.WorkOrders;

namespace FieldOps.Application.WorkOrders;

public sealed class WorkOrderService(IWorkOrderRepository repository, TimeProvider clock)
{
    public async Task<IReadOnlyCollection<WorkOrderView>> ListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var orders = await repository.ListAsync(organizationId, cancellationToken);
        return orders.OrderByDescending(item => item.UpdatedAtUtc).Select(item => item.ToView()).ToArray();
    }

    public async Task<WorkOrderView> CreateAsync(CommandContext context, CreateWorkOrder request, CancellationToken cancellationToken)
    {
        var order = WorkOrder.Submit(
            Guid.NewGuid(), context.OrganizationId, request.Title, request.Description,
            context.ActorId, request.Priority, context.CommandId, clock.GetUtcNow());

        await repository.AddAsync(order, cancellationToken);
        return order.ToView();
    }

    public Task<WorkOrderView> TriageAsync(CommandContext context, Guid id, WorkOrderPriority priority, int slaHours, CancellationToken cancellationToken) =>
        MutateAsync(context, id, order => order.Triage(priority, clock.GetUtcNow().AddHours(slaHours), context.ActorId, context.CommandId, clock.GetUtcNow()), cancellationToken);

    public Task<WorkOrderView> OfferAsync(CommandContext context, Guid id, Guid contractorId, int responseMinutes, CancellationToken cancellationToken) =>
        MutateAsync(context, id, order => order.OfferTo(contractorId, clock.GetUtcNow().AddMinutes(responseMinutes), context.ActorId, context.CommandId, clock.GetUtcNow()), cancellationToken);

    public Task<WorkOrderView> AcceptAsync(CommandContext context, Guid id, CancellationToken cancellationToken) =>
        MutateAsync(context, id, order => order.AcceptOffer(context.ActorId, context.CommandId, clock.GetUtcNow()), cancellationToken);

    public Task<WorkOrderView> ScheduleAsync(CommandContext context, Guid id, DateTimeOffset scheduledForUtc, CancellationToken cancellationToken) =>
        MutateAsync(context, id, order => order.Schedule(context.ActorId, scheduledForUtc, context.CommandId, clock.GetUtcNow()), cancellationToken);

    public Task<WorkOrderView> StartAsync(CommandContext context, Guid id, CancellationToken cancellationToken) =>
        MutateAsync(context, id, order => order.Start(context.ActorId, context.CommandId, clock.GetUtcNow()), cancellationToken);

    public Task<WorkOrderView> SubmitForReviewAsync(CommandContext context, Guid id, CancellationToken cancellationToken) =>
        MutateAsync(context, id, order => order.SubmitForReview(context.ActorId, context.CommandId, clock.GetUtcNow()), cancellationToken);

    public Task<WorkOrderView> CloseAsync(CommandContext context, Guid id, CancellationToken cancellationToken) =>
        MutateAsync(context, id, order => order.Close(context.ActorId, context.CommandId, clock.GetUtcNow()), cancellationToken);

    public Task<WorkOrderView> MarkNoResponseAsync(CommandContext context, Guid id, CancellationToken cancellationToken) =>
        MutateAsync(context, id, order => order.MarkNoResponse(context.ActorId, context.CommandId, clock.GetUtcNow()), cancellationToken);

    public Task<WorkOrderView> MarkNoShowAsync(CommandContext context, Guid id, string reason, CancellationToken cancellationToken) =>
        MutateAsync(context, id, order => order.MarkNoShow(context.ActorId, reason, context.CommandId, clock.GetUtcNow()), cancellationToken);

    private async Task<WorkOrderView> MutateAsync(
        CommandContext context,
        Guid id,
        Action<WorkOrder> action,
        CancellationToken cancellationToken)
    {
        var order = await repository.GetAsync(context.OrganizationId, id, cancellationToken)
            ?? throw new KeyNotFoundException($"Work order {id} was not found.");

        action(order);
        await repository.SaveAsync(order, cancellationToken);
        return order.ToView();
    }
}

