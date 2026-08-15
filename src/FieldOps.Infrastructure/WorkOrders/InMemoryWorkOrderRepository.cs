using System.Collections.Concurrent;
using FieldOps.Application.WorkOrders;
using FieldOps.Domain.WorkOrders;

namespace FieldOps.Infrastructure.WorkOrders;

public sealed class InMemoryWorkOrderRepository : IWorkOrderRepository
{
    private readonly ConcurrentDictionary<(Guid OrganizationId, Guid Id), WorkOrder> _orders = new();

    public Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_orders.TryAdd((workOrder.OrganizationId, workOrder.Id), workOrder))
            throw new InvalidOperationException($"Work order {workOrder.Id} already exists.");

        return Task.CompletedTask;
    }

    public Task<WorkOrder?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _orders.TryGetValue((organizationId, id), out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyCollection<WorkOrder>> ListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<WorkOrder> result = _orders.Values
            .Where(item => item.OrganizationId == organizationId)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task SaveAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _orders[(workOrder.OrganizationId, workOrder.Id)] = workOrder;
        return Task.CompletedTask;
    }
}

