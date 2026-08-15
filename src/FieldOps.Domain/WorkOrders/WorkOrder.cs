using FieldOps.Domain;

namespace FieldOps.Domain.WorkOrders;

public sealed class WorkOrder
{
    private readonly List<WorkOrderEvent> _events = [];

    private WorkOrder(
        Guid id,
        Guid organizationId,
        string title,
        string description,
        Guid requesterId,
        WorkOrderPriority priority,
        DateTimeOffset createdAtUtc)
    {
        Id = DomainGuard.RequiredId(id, nameof(id));
        OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId));
        RequesterId = DomainGuard.RequiredId(requesterId, nameof(requesterId));
        Title = DomainGuard.RequiredText(title, nameof(title), 160);
        Description = DomainGuard.RequiredText(description, nameof(description), 4_000);
        Priority = priority;
        Status = WorkOrderStatus.Submitted;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public Guid RequesterId { get; }
    public string Title { get; }
    public string Description { get; }
    public WorkOrderPriority Priority { get; private set; }
    public WorkOrderStatus Status { get; private set; }
    public Guid? ContractorId { get; private set; }
    public DateTimeOffset? OfferExpiresAtUtc { get; private set; }
    public DateTimeOffset? ScheduledForUtc { get; private set; }
    public DateTimeOffset? SlaDueAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyList<WorkOrderEvent> Events => _events;

    public static WorkOrder Submit(
        Guid id,
        Guid organizationId,
        string title,
        string description,
        Guid requesterId,
        WorkOrderPriority priority,
        Guid commandId,
        DateTimeOffset nowUtc)
    {
        var workOrder = new WorkOrder(id, organizationId, title, description, requesterId, priority, nowUtc);
        workOrder.Record(commandId, requesterId, "WorkOrderSubmitted", WorkOrderStatus.Submitted, null, nowUtc);
        return workOrder;
    }

    public void Triage(
        WorkOrderPriority priority,
        DateTimeOffset slaDueAtUtc,
        Guid actorId,
        Guid commandId,
        DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.Submitted, WorkOrderStatus.Reopened);
        var validSlaDueAtUtc = DomainGuard.Future(slaDueAtUtc, nowUtc, "sla.invalid", "The SLA due date must be in the future.");
        Priority = priority;
        SlaDueAtUtc = validSlaDueAtUtc;
        Transition(WorkOrderStatus.Triaged, actorId, commandId, "WorkOrderTriaged", null, nowUtc);
    }

    public void OfferTo(
        Guid contractorId,
        DateTimeOffset expiresAtUtc,
        Guid actorId,
        Guid commandId,
        DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.Triaged, WorkOrderStatus.Declined, WorkOrderStatus.NoResponse, WorkOrderStatus.NoShow);
        var validContractorId = DomainGuard.RequiredId(contractorId, nameof(contractorId));
        var validExpiresAtUtc = DomainGuard.Future(expiresAtUtc, nowUtc, "offer.expiry.invalid", "The offer expiry must be in the future.");
        ContractorId = validContractorId;
        OfferExpiresAtUtc = validExpiresAtUtc;
        ScheduledForUtc = null;
        Transition(WorkOrderStatus.Offered, actorId, commandId, "AssignmentOffered", null, nowUtc);
    }

    public void AcceptOffer(Guid contractorId, Guid commandId, DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.Offered);
        RequireAssignedContractor(contractorId);
        if (OfferExpiresAtUtc <= nowUtc)
            throw new WorkOrderException("offer.expired", "The assignment offer has expired.");

        Transition(WorkOrderStatus.Accepted, contractorId, commandId, "AssignmentAccepted", null, nowUtc);
    }

    public void DeclineOffer(Guid contractorId, string reason, Guid commandId, DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.Offered);
        RequireAssignedContractor(contractorId);
        Transition(WorkOrderStatus.Declined, contractorId, commandId, "AssignmentDeclined", DomainGuard.RequiredText(reason, nameof(reason), 500), nowUtc);
    }

    public void MarkNoResponse(Guid actorId, Guid commandId, DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.Offered);
        if (OfferExpiresAtUtc is null || OfferExpiresAtUtc > nowUtc)
            throw new WorkOrderException("offer.active", "The response window is still active.");

        Transition(WorkOrderStatus.NoResponse, actorId, commandId, "AssignmentTimedOut", "No response before the deadline.", nowUtc);
    }

    public void Schedule(Guid contractorId, DateTimeOffset scheduledForUtc, Guid commandId, DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.Accepted);
        RequireAssignedContractor(contractorId);
        ScheduledForUtc = DomainGuard.Future(scheduledForUtc, nowUtc, "schedule.invalid", "The appointment must be in the future.");
        Transition(WorkOrderStatus.Scheduled, contractorId, commandId, "AppointmentScheduled", null, nowUtc);
    }

    public void Start(Guid contractorId, Guid commandId, DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.Scheduled);
        RequireAssignedContractor(contractorId);
        if (ScheduledForUtc is null || ScheduledForUtc > nowUtc)
            throw new WorkOrderException("appointment.not_started", "The scheduled start time has not been reached.");

        Transition(WorkOrderStatus.InProgress, contractorId, commandId, "WorkStarted", null, nowUtc);
    }

    public void MarkNoShow(Guid actorId, string reason, Guid commandId, DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.Scheduled);
        if (ScheduledForUtc is null || ScheduledForUtc > nowUtc)
            throw new WorkOrderException("appointment.active", "The scheduled time has not passed.");
        Transition(WorkOrderStatus.NoShow, actorId, commandId, "ContractorNoShow", DomainGuard.RequiredText(reason, nameof(reason), 500), nowUtc);
    }

    public void SubmitForReview(Guid contractorId, Guid commandId, DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.InProgress);
        RequireAssignedContractor(contractorId);
        Transition(WorkOrderStatus.ReadyForReview, contractorId, commandId, "WorkSubmittedForReview", null, nowUtc);
    }

    public void Close(Guid actorId, Guid commandId, DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.ReadyForReview);
        Transition(WorkOrderStatus.Closed, actorId, commandId, "WorkOrderClosed", null, nowUtc);
    }

    public void Reopen(Guid actorId, string reason, Guid commandId, DateTimeOffset nowUtc)
    {
        if (AlreadyProcessed(commandId)) return;
        RequireStatus(WorkOrderStatus.Closed, WorkOrderStatus.ReadyForReview);
        Transition(WorkOrderStatus.Reopened, actorId, commandId, "WorkOrderReopened", DomainGuard.RequiredText(reason, nameof(reason), 500), nowUtc);
    }

    private bool AlreadyProcessed(Guid commandId)
    {
        DomainGuard.RequiredId(commandId, nameof(commandId));
        return _events.Exists(item => item.CommandId == commandId);
    }

    private void RequireAssignedContractor(Guid contractorId)
    {
        DomainGuard.RequiredId(contractorId, nameof(contractorId));
        if (ContractorId != contractorId)
            throw new WorkOrderException("assignment.forbidden", "Only the assigned contractor may perform this action.");
    }

    private void RequireStatus(params WorkOrderStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new WorkOrderException("transition.invalid", $"Status {Status} cannot perform this transition.");
    }

    private void Transition(
        WorkOrderStatus next,
        Guid actorId,
        Guid commandId,
        string eventType,
        string? reason,
        DateTimeOffset nowUtc)
    {
        DomainGuard.RequiredId(commandId, nameof(commandId));
        DomainGuard.RequiredId(actorId, nameof(actorId));
        var previous = Status;
        Status = next;
        UpdatedAtUtc = nowUtc;
        Record(commandId, actorId, eventType, previous, reason, nowUtc);
    }

    private void Record(
        Guid commandId,
        Guid actorId,
        string eventType,
        WorkOrderStatus from,
        string? reason,
        DateTimeOffset nowUtc)
    {
        DomainGuard.RequiredId(commandId, nameof(commandId));
        DomainGuard.RequiredId(actorId, nameof(actorId));
        Version++;
        _events.Add(new WorkOrderEvent(
            Guid.NewGuid(), Id, OrganizationId, actorId, commandId, eventType,
            from, Status, reason, nowUtc, Version));
    }

}
