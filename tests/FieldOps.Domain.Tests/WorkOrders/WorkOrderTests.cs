using FieldOps.Domain.WorkOrders;

namespace FieldOps.Domain.Tests.WorkOrders;

public sealed class WorkOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 14, 0, 0, TimeSpan.Zero);
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _dispatcherId = Guid.NewGuid();
    private readonly Guid _contractorId = Guid.NewGuid();

    [Fact]
    public void Critical_path_reaches_closed_and_preserves_audit_history()
    {
        var order = CreateSubmitted();

        order.Triage(WorkOrderPriority.P1, Now.AddHours(4), _dispatcherId, Guid.NewGuid(), Now.AddMinutes(1));
        order.OfferTo(_contractorId, Now.AddMinutes(30), _dispatcherId, Guid.NewGuid(), Now.AddMinutes(2));
        order.AcceptOffer(_contractorId, Guid.NewGuid(), Now.AddMinutes(3));
        order.Schedule(_contractorId, Now.AddHours(1), Guid.NewGuid(), Now.AddMinutes(4));
        order.Start(_contractorId, Guid.NewGuid(), Now.AddHours(1));
        order.SubmitForReview(_contractorId, Guid.NewGuid(), Now.AddHours(2));
        order.Close(_dispatcherId, Guid.NewGuid(), Now.AddHours(2).AddMinutes(5));

        Assert.Equal(WorkOrderStatus.Closed, order.Status);
        Assert.Equal(8, order.Version);
        Assert.Equal(8, order.Events.Count);
        Assert.Equal("WorkOrderClosed", order.Events[^1].EventType);
    }

    [Fact]
    public void Same_command_is_idempotent()
    {
        var order = CreateSubmitted();
        var commandId = Guid.NewGuid();

        order.Triage(WorkOrderPriority.P2, Now.AddHours(8), _dispatcherId, commandId, Now.AddMinutes(1));
        order.Triage(WorkOrderPriority.P2, Now.AddHours(8), _dispatcherId, commandId, Now.AddMinutes(1));

        Assert.Equal(2, order.Version);
        Assert.Equal(2, order.Events.Count);
    }

    [Fact]
    public void Unassigned_contractor_cannot_accept_offer()
    {
        var order = CreateSubmitted();
        order.Triage(WorkOrderPriority.P2, Now.AddHours(8), _dispatcherId, Guid.NewGuid(), Now.AddMinutes(1));
        order.OfferTo(_contractorId, Now.AddMinutes(30), _dispatcherId, Guid.NewGuid(), Now.AddMinutes(2));

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.AcceptOffer(Guid.NewGuid(), Guid.NewGuid(), Now.AddMinutes(3)));

        Assert.Equal("assignment.forbidden", exception.Code);
    }

    [Fact]
    public void Expired_offer_becomes_no_response_and_can_be_reassigned()
    {
        var order = CreateSubmitted();
        order.Triage(WorkOrderPriority.P1, Now.AddHours(4), _dispatcherId, Guid.NewGuid(), Now.AddMinutes(1));
        order.OfferTo(_contractorId, Now.AddMinutes(10), _dispatcherId, Guid.NewGuid(), Now.AddMinutes(2));
        order.MarkNoResponse(_dispatcherId, Guid.NewGuid(), Now.AddMinutes(11));

        var replacementId = Guid.NewGuid();
        order.OfferTo(replacementId, Now.AddMinutes(40), _dispatcherId, Guid.NewGuid(), Now.AddMinutes(12));

        Assert.Equal(WorkOrderStatus.Offered, order.Status);
        Assert.Equal(replacementId, order.ContractorId);
        Assert.Contains(order.Events, item => item.EventType == "AssignmentTimedOut");
    }

    [Fact]
    public void Appointment_cannot_be_scheduled_in_the_past()
    {
        var order = CreateAccepted();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.Schedule(_contractorId, Now, Guid.NewGuid(), Now.AddMinutes(5)));

        Assert.Equal("schedule.invalid", exception.Code);
    }

    private WorkOrder CreateSubmitted() => WorkOrder.Submit(
        Guid.NewGuid(), _organizationId, "Water leak", "Leak detected in the mechanical room.",
        _requesterId, WorkOrderPriority.P1, Guid.NewGuid(), Now);

    private WorkOrder CreateAccepted()
    {
        var order = CreateSubmitted();
        order.Triage(WorkOrderPriority.P1, Now.AddHours(4), _dispatcherId, Guid.NewGuid(), Now.AddMinutes(1));
        order.OfferTo(_contractorId, Now.AddMinutes(30), _dispatcherId, Guid.NewGuid(), Now.AddMinutes(2));
        order.AcceptOffer(_contractorId, Guid.NewGuid(), Now.AddMinutes(3));
        return order;
    }
}
