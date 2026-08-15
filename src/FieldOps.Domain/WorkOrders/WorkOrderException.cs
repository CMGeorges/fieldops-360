namespace FieldOps.Domain.WorkOrders;

public sealed class WorkOrderException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
