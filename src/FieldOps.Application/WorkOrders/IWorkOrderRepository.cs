using FieldOps.Domain.WorkOrders;

namespace FieldOps.Application.WorkOrders;

public interface IWorkOrderRepository
{
    Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken);
    Task<WorkOrder?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<WorkOrder>> ListAsync(Guid organizationId, CancellationToken cancellationToken);
    Task SaveAsync(WorkOrder workOrder, CancellationToken cancellationToken);
}

