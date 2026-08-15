using FieldOps.Domain.WorkOrders;

namespace FieldOps.Domain.Tests.WorkOrders;

public sealed class WorkOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RequesterId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DispatcherId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ContractorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Critical_path_reaches_closed_and_preserves_audit_history()
    {
        var order = Submitted();

        order.Triage(WorkOrderPriority.P1, Now.AddHours(4), DispatcherId, NewId(), Now.AddMinutes(1));
        order.OfferTo(ContractorId, Now.AddMinutes(30), DispatcherId, NewId(), Now.AddMinutes(2));
        order.AcceptOffer(ContractorId, NewId(), Now.AddMinutes(3));
        order.Schedule(ContractorId, Now.AddHours(1), NewId(), Now.AddMinutes(4));
        order.Start(ContractorId, NewId(), Now.AddHours(1));
        order.SubmitForReview(ContractorId, NewId(), Now.AddHours(2));
        order.Close(DispatcherId, NewId(), Now.AddHours(2).AddMinutes(5));

        Assert.Equal(WorkOrderStatus.Closed, order.Status);
        Assert.Equal(8, order.Version);
        Assert.Equal(8, order.Events.Count);
        Assert.Equal("WorkOrderClosed", order.Events[^1].EventType);
    }

    [Fact]
    public void Submission_records_complete_audit_context()
    {
        var commandId = NewId();
        var orderId = NewId();

        var order = WorkOrder.Submit(
            orderId, OrganizationId, "  Water leak  ", "  Leak in the mechanical room.  ",
            RequesterId, WorkOrderPriority.P1, commandId, Now);

        var audit = Assert.Single(order.Events);
        Assert.Equal(orderId, audit.WorkOrderId);
        Assert.Equal(OrganizationId, audit.OrganizationId);
        Assert.Equal(RequesterId, audit.ActorId);
        Assert.Equal(commandId, audit.CommandId);
        Assert.Equal(WorkOrderStatus.Submitted, audit.From);
        Assert.Equal(WorkOrderStatus.Submitted, audit.To);
        Assert.Equal(Now, audit.OccurredAtUtc);
        Assert.Equal(1, audit.Version);
        Assert.Equal("Water leak", order.Title);
        Assert.Equal("Leak in the mechanical room.", order.Description);
        Assert.Equal(RequesterId, order.RequesterId);
        Assert.Equal(Now, order.CreatedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Submission_rejects_empty_entity_identifiers(int identifier)
    {
        var id = identifier == 0 ? Guid.Empty : NewId();
        var organizationId = identifier == 1 ? Guid.Empty : OrganizationId;
        var requesterId = identifier == 2 ? Guid.Empty : RequesterId;

        Assert.Throws<ArgumentException>(() => WorkOrder.Submit(
            id, organizationId, "Water leak", "Leak detected.", requesterId,
            WorkOrderPriority.P1, NewId(), Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Submission_rejects_blank_title(string title)
    {
        Assert.Throws<ArgumentException>(() => WorkOrder.Submit(
            NewId(), OrganizationId, title, "Leak detected.", RequesterId,
            WorkOrderPriority.P1, NewId(), Now));
    }

    [Fact]
    public void Submission_rejects_null_title()
    {
        Assert.Throws<ArgumentException>(() => WorkOrder.Submit(
            NewId(), OrganizationId, null!, "Leak detected.", RequesterId,
            WorkOrderPriority.P1, NewId(), Now));
    }

    [Fact]
    public void Submission_rejects_oversized_description()
    {
        Assert.Throws<ArgumentException>(() => WorkOrder.Submit(
            NewId(), OrganizationId, "Water leak", new string('x', 4_001), RequesterId,
            WorkOrderPriority.P1, NewId(), Now));
    }

    [Fact]
    public void Submission_rejects_empty_command_identifier()
    {
        Assert.Throws<ArgumentException>(() => WorkOrder.Submit(
            NewId(), OrganizationId, "Water leak", "Leak detected.", RequesterId,
            WorkOrderPriority.P1, Guid.Empty, Now));
    }

    [Fact]
    public void Same_command_is_idempotent()
    {
        var order = Submitted();
        var commandId = NewId();

        order.Triage(WorkOrderPriority.P2, Now.AddHours(8), DispatcherId, commandId, Now.AddMinutes(1));
        order.Triage(WorkOrderPriority.P2, Now.AddHours(8), DispatcherId, commandId, Now.AddMinutes(1));

        Assert.Equal(2, order.Version);
        Assert.Equal(2, order.Events.Count);
    }

    [Fact]
    public void Every_transition_ignores_a_duplicate_command()
    {
        AssertDuplicateIsIgnored(Triaged(), (order, command) =>
            order.OfferTo(ContractorId, Now.AddMinutes(30), DispatcherId, command, Now.AddMinutes(2)));

        AssertDuplicateIsIgnored(Offered(), (order, command) =>
            order.AcceptOffer(ContractorId, command, Now.AddMinutes(3)));

        AssertDuplicateIsIgnored(Offered(), (order, command) =>
            order.DeclineOffer(ContractorId, "Unavailable", command, Now.AddMinutes(3)));

        AssertDuplicateIsIgnored(Offered(Now.AddMinutes(10)), (order, command) =>
            order.MarkNoResponse(DispatcherId, command, Now.AddMinutes(11)));

        AssertDuplicateIsIgnored(Accepted(), (order, command) =>
            order.Schedule(ContractorId, Now.AddHours(1), command, Now.AddMinutes(4)));

        AssertDuplicateIsIgnored(Scheduled(), (order, command) =>
            order.Start(ContractorId, command, Now.AddHours(1)));

        AssertDuplicateIsIgnored(Scheduled(), (order, command) =>
            order.MarkNoShow(DispatcherId, "Unavailable", command, Now.AddHours(1)));

        AssertDuplicateIsIgnored(InProgress(), (order, command) =>
            order.SubmitForReview(ContractorId, command, Now.AddHours(2)));

        AssertDuplicateIsIgnored(ReadyForReview(), (order, command) =>
            order.Close(DispatcherId, command, Now.AddHours(2).AddMinutes(5)));

        AssertDuplicateIsIgnored(Closed(), (order, command) =>
            order.Reopen(DispatcherId, "Problem returned", command, Now.AddHours(3)));
    }

    [Fact]
    public void Triage_rejects_expired_sla()
    {
        var order = Submitted();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.Triage(WorkOrderPriority.P2, Now, DispatcherId, NewId(), Now.AddMinutes(1)));

        Assert.Equal("sla.invalid", exception.Code);
        Assert.Equal(WorkOrderPriority.P1, order.Priority);
        Assert.Null(order.SlaDueAtUtc);
        Assert.Equal(WorkOrderStatus.Submitted, order.Status);
    }

    [Fact]
    public void Invalid_transition_returns_stable_error_code()
    {
        var order = Submitted();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.Close(DispatcherId, NewId(), Now.AddMinutes(1)));

        Assert.Equal("transition.invalid", exception.Code);
    }

    [Fact]
    public void Offer_rejects_empty_contractor_identifier()
    {
        var order = Triaged();

        Assert.Throws<ArgumentException>(() =>
            order.OfferTo(Guid.Empty, Now.AddMinutes(30), DispatcherId, NewId(), Now.AddMinutes(2)));
    }

    [Fact]
    public void Offer_rejects_expired_response_window()
    {
        var order = Triaged();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.OfferTo(ContractorId, Now, DispatcherId, NewId(), Now.AddMinutes(2)));

        Assert.Equal("offer.expiry.invalid", exception.Code);
        Assert.Null(order.ContractorId);
        Assert.Null(order.OfferExpiresAtUtc);
        Assert.Equal(WorkOrderStatus.Triaged, order.Status);
    }

    [Fact]
    public void Unassigned_contractor_cannot_accept_offer()
    {
        var order = Offered();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.AcceptOffer(NewId(), NewId(), Now.AddMinutes(3)));

        Assert.Equal("assignment.forbidden", exception.Code);
    }

    [Fact]
    public void Expired_offer_cannot_be_accepted()
    {
        var order = Offered(Now.AddMinutes(10));

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.AcceptOffer(ContractorId, NewId(), Now.AddMinutes(10)));

        Assert.Equal("offer.expired", exception.Code);
    }

    [Fact]
    public void Declined_offer_can_be_reassigned_and_preserves_reason()
    {
        var order = Offered();
        order.DeclineOffer(ContractorId, "Outside service area", NewId(), Now.AddMinutes(3));
        var replacement = NewId();

        order.OfferTo(replacement, Now.AddHours(1), DispatcherId, NewId(), Now.AddMinutes(4));

        Assert.Equal(WorkOrderStatus.Offered, order.Status);
        Assert.Equal(replacement, order.ContractorId);
        Assert.Contains(order.Events, item => item.EventType == "AssignmentDeclined" && item.Reason == "Outside service area");
    }

    [Fact]
    public void Decline_requires_a_reason()
    {
        var order = Offered();

        Assert.Throws<ArgumentException>(() =>
            order.DeclineOffer(ContractorId, " ", NewId(), Now.AddMinutes(3)));
    }

    [Fact]
    public void Active_offer_cannot_be_marked_no_response()
    {
        var order = Offered(Now.AddMinutes(30));

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.MarkNoResponse(DispatcherId, NewId(), Now.AddMinutes(10)));

        Assert.Equal("offer.active", exception.Code);
    }

    [Fact]
    public void Expired_offer_becomes_no_response_and_can_be_reassigned()
    {
        var order = Offered(Now.AddMinutes(10));
        order.MarkNoResponse(DispatcherId, NewId(), Now.AddMinutes(11));
        var replacement = NewId();

        order.OfferTo(replacement, Now.AddMinutes(40), DispatcherId, NewId(), Now.AddMinutes(12));

        Assert.Equal(WorkOrderStatus.Offered, order.Status);
        Assert.Equal(replacement, order.ContractorId);
        Assert.Contains(order.Events, item => item.EventType == "AssignmentTimedOut");
    }

    [Fact]
    public void Appointment_cannot_be_scheduled_in_the_past()
    {
        var order = Accepted();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.Schedule(ContractorId, Now, NewId(), Now.AddMinutes(5)));

        Assert.Equal("schedule.invalid", exception.Code);
    }

    [Fact]
    public void Unassigned_contractor_cannot_schedule()
    {
        var order = Accepted();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.Schedule(NewId(), Now.AddHours(1), NewId(), Now.AddMinutes(5)));

        Assert.Equal("assignment.forbidden", exception.Code);
    }

    [Fact]
    public void Work_cannot_start_before_scheduled_time()
    {
        var order = Scheduled();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.Start(ContractorId, NewId(), Now.AddMinutes(30)));

        Assert.Equal("appointment.not_started", exception.Code);
    }

    [Fact]
    public void Unassigned_contractor_cannot_start_work()
    {
        var order = Scheduled();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.Start(NewId(), NewId(), Now.AddHours(1)));

        Assert.Equal("assignment.forbidden", exception.Code);
    }

    [Fact]
    public void No_show_cannot_be_recorded_before_appointment()
    {
        var order = Scheduled();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.MarkNoShow(DispatcherId, "Contractor absent", NewId(), Now.AddMinutes(30)));

        Assert.Equal("appointment.active", exception.Code);
    }

    [Fact]
    public void No_show_can_be_reassigned_after_appointment()
    {
        var order = Scheduled();
        order.MarkNoShow(DispatcherId, "Contractor absent", NewId(), Now.AddHours(1));
        var replacement = NewId();

        order.OfferTo(replacement, Now.AddHours(2), DispatcherId, NewId(), Now.AddHours(1).AddMinutes(1));

        Assert.Equal(WorkOrderStatus.Offered, order.Status);
        Assert.Equal(replacement, order.ContractorId);
        Assert.Contains(order.Events, item => item.EventType == "ContractorNoShow" && item.Reason == "Contractor absent");
    }

    [Fact]
    public void No_show_requires_a_reason()
    {
        var order = Scheduled();

        Assert.Throws<ArgumentException>(() =>
            order.MarkNoShow(DispatcherId, string.Empty, NewId(), Now.AddHours(1)));
    }

    [Fact]
    public void Unassigned_contractor_cannot_submit_for_review()
    {
        var order = InProgress();

        var exception = Assert.Throws<WorkOrderException>(() =>
            order.SubmitForReview(NewId(), NewId(), Now.AddHours(2)));

        Assert.Equal("assignment.forbidden", exception.Code);
    }

    [Fact]
    public void Closed_order_can_be_reopened_retriaged_and_reassigned()
    {
        var order = Closed();
        order.Reopen(DispatcherId, "Problem returned", NewId(), Now.AddHours(3));
        order.Triage(WorkOrderPriority.P1, Now.AddHours(6), DispatcherId, NewId(), Now.AddHours(3).AddMinutes(1));
        var replacement = NewId();

        order.OfferTo(replacement, Now.AddHours(4), DispatcherId, NewId(), Now.AddHours(3).AddMinutes(2));

        Assert.Equal(WorkOrderStatus.Offered, order.Status);
        Assert.Equal(replacement, order.ContractorId);
        Assert.Contains(order.Events, item => item.EventType == "WorkOrderReopened" && item.Reason == "Problem returned");
    }

    [Fact]
    public void Ready_for_review_order_can_be_reopened()
    {
        var order = ReadyForReview();

        order.Reopen(DispatcherId, "Evidence incomplete", NewId(), Now.AddHours(3));

        Assert.Equal(WorkOrderStatus.Reopened, order.Status);
    }

    [Fact]
    public void Reopen_requires_a_reason()
    {
        var order = Closed();

        Assert.Throws<ArgumentException>(() =>
            order.Reopen(DispatcherId, string.Empty, NewId(), Now.AddHours(3)));
    }

    [Fact]
    public void Transition_rejects_empty_actor_identifier()
    {
        var order = Submitted();

        Assert.Throws<ArgumentException>(() =>
            order.Triage(WorkOrderPriority.P2, Now.AddHours(4), Guid.Empty, NewId(), Now.AddMinutes(1)));

        Assert.Equal(WorkOrderStatus.Submitted, order.Status);
        Assert.Equal(1, order.Version);
    }

    private static WorkOrder Submitted() => WorkOrder.Submit(
        NewId(), OrganizationId, "Water leak", "Leak detected in the mechanical room.",
        RequesterId, WorkOrderPriority.P1, NewId(), Now);

    private static WorkOrder Triaged()
    {
        var order = Submitted();
        order.Triage(WorkOrderPriority.P1, Now.AddHours(4), DispatcherId, NewId(), Now.AddMinutes(1));
        return order;
    }

    private static WorkOrder Offered(DateTimeOffset? expiresAtUtc = null)
    {
        var order = Triaged();
        order.OfferTo(ContractorId, expiresAtUtc ?? Now.AddMinutes(30), DispatcherId, NewId(), Now.AddMinutes(2));
        return order;
    }

    private static WorkOrder Accepted()
    {
        var order = Offered();
        order.AcceptOffer(ContractorId, NewId(), Now.AddMinutes(3));
        return order;
    }

    private static WorkOrder Scheduled()
    {
        var order = Accepted();
        order.Schedule(ContractorId, Now.AddHours(1), NewId(), Now.AddMinutes(4));
        return order;
    }

    private static WorkOrder InProgress()
    {
        var order = Scheduled();
        order.Start(ContractorId, NewId(), Now.AddHours(1));
        return order;
    }

    private static WorkOrder ReadyForReview()
    {
        var order = InProgress();
        order.SubmitForReview(ContractorId, NewId(), Now.AddHours(2));
        return order;
    }

    private static WorkOrder Closed()
    {
        var order = ReadyForReview();
        order.Close(DispatcherId, NewId(), Now.AddHours(2).AddMinutes(5));
        return order;
    }

    private static void AssertDuplicateIsIgnored(WorkOrder order, Action<WorkOrder, Guid> transition)
    {
        var commandId = NewId();
        transition(order, commandId);
        var version = order.Version;
        var events = order.Events.Count;

        transition(order, commandId);

        Assert.Equal(version, order.Version);
        Assert.Equal(events, order.Events.Count);
    }

    private static Guid NewId() => Guid.NewGuid();
}
