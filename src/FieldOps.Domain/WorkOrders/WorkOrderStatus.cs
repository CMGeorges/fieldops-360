namespace FieldOps.Domain.WorkOrders;

public enum WorkOrderStatus
{
    Submitted,
    Triaged,
    Offered,
    Accepted,
    Scheduled,
    InProgress,
    ReadyForReview,
    Closed,
    Declined,
    NoResponse,
    NoShow,
    Cancelled,
    Reopened
}

public enum WorkOrderPriority
{
    P1,
    P2,
    P3,
    P4
}
