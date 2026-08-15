using FieldOps.Application.WorkOrders;
using FieldOps.Domain.WorkOrders;
using FieldOps.Infrastructure.WorkOrders;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IWorkOrderRepository, InMemoryWorkOrderRepository>();
builder.Services.AddSingleton<WorkOrderService>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration["FIELDOPS_WEB_ORIGIN"] ?? "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception exception) when (exception is WorkOrderException or ArgumentException or KeyNotFoundException)
    {
        var status = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status409Conflict
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://fieldops.local/problems/domain-rule",
            title = "The operation could not be completed.",
            status,
            detail = exception.Message,
            code = (exception as WorkOrderException)?.Code,
            traceId = context.TraceIdentifier
        });
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "fieldops-api" }));

var api = app.MapGroup("/api/work-orders");

api.MapGet("/", async (HttpContext http, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(DemoIdentity.OrganizationId, cancellationToken)));

api.MapPost("/", async (HttpContext http, CreateRequest request, WorkOrderService service, CancellationToken cancellationToken) =>
{
    var context = DemoIdentity.Context(http);
    var created = await service.CreateAsync(context, new CreateWorkOrder(request.Title, request.Description, request.Priority), cancellationToken);
    return Results.Created($"/api/work-orders/{created.Id}", created);
});

api.MapPost("/{id:guid}/triage", async (Guid id, HttpContext http, TriageRequest request, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.TriageAsync(DemoIdentity.Context(http), id, request.Priority, request.SlaHours, cancellationToken)));

api.MapPost("/{id:guid}/offer", async (Guid id, HttpContext http, OfferRequest request, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.OfferAsync(DemoIdentity.Context(http), id, request.ContractorId, request.ResponseMinutes, cancellationToken)));

api.MapPost("/{id:guid}/accept", async (Guid id, HttpContext http, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.AcceptAsync(DemoIdentity.Context(http), id, cancellationToken)));

api.MapPost("/{id:guid}/schedule", async (Guid id, HttpContext http, ScheduleRequest request, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.ScheduleAsync(DemoIdentity.Context(http), id, request.ScheduledForUtc, cancellationToken)));

api.MapPost("/{id:guid}/start", async (Guid id, HttpContext http, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.StartAsync(DemoIdentity.Context(http), id, cancellationToken)));

api.MapPost("/{id:guid}/review", async (Guid id, HttpContext http, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.SubmitForReviewAsync(DemoIdentity.Context(http), id, cancellationToken)));

api.MapPost("/{id:guid}/close", async (Guid id, HttpContext http, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.CloseAsync(DemoIdentity.Context(http), id, cancellationToken)));

api.MapPost("/{id:guid}/no-response", async (Guid id, HttpContext http, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.MarkNoResponseAsync(DemoIdentity.Context(http), id, cancellationToken)));

api.MapPost("/{id:guid}/no-show", async (Guid id, HttpContext http, ReasonRequest request, WorkOrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.MarkNoShowAsync(DemoIdentity.Context(http), id, request.Reason, cancellationToken)));

await DemoData.SeedAsync(app.Services);
await app.RunAsync();

public partial class Program { }

internal static class DemoIdentity
{
    public static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DispatcherId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ContractorId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static CommandContext Context(HttpContext http)
    {
        var actorId = Guid.TryParse(http.Request.Headers["X-Actor-Id"], out var actor) ? actor : DispatcherId;
        var commandId = Guid.TryParse(http.Request.Headers["Idempotency-Key"], out var command) ? command : Guid.NewGuid();
        return new CommandContext(OrganizationId, actorId, commandId);
    }
}

internal static class DemoData
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var repository = services.GetRequiredService<IWorkOrderRepository>();
        var clock = services.GetRequiredService<TimeProvider>();
        var now = clock.GetUtcNow();

        var submitted = WorkOrder.Submit(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), DemoIdentity.OrganizationId,
            "Fuite dans la salle mécanique", "Présence d'eau près de la pompe principale.",
            DemoIdentity.DispatcherId, WorkOrderPriority.P1, Guid.NewGuid(), now.AddMinutes(-18));
        await repository.AddAsync(submitted, CancellationToken.None);

        var offered = WorkOrder.Submit(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), DemoIdentity.OrganizationId,
            "Porte d'entrée désalignée", "La porte frotte et ne verrouille plus correctement.",
            DemoIdentity.DispatcherId, WorkOrderPriority.P2, Guid.NewGuid(), now.AddHours(-2));
        offered.Triage(WorkOrderPriority.P2, now.AddHours(6), DemoIdentity.DispatcherId, Guid.NewGuid(), now.AddHours(-1));
        offered.OfferTo(DemoIdentity.ContractorId, now.AddHours(1), DemoIdentity.DispatcherId, Guid.NewGuid(), now.AddMinutes(-30));
        await repository.AddAsync(offered, CancellationToken.None);

        var active = WorkOrder.Submit(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), DemoIdentity.OrganizationId,
            "Inspection préventive HVAC", "Inspection trimestrielle et remplacement des filtres.",
            DemoIdentity.DispatcherId, WorkOrderPriority.P3, Guid.NewGuid(), now.AddDays(-1));
        active.Triage(WorkOrderPriority.P3, now.AddDays(2), DemoIdentity.DispatcherId, Guid.NewGuid(), now.AddHours(-20));
        active.OfferTo(DemoIdentity.ContractorId, now.AddHours(-18), DemoIdentity.DispatcherId, Guid.NewGuid(), now.AddHours(-19));
        active.AcceptOffer(DemoIdentity.ContractorId, Guid.NewGuid(), now.AddHours(-18).AddMinutes(-30));
        active.Schedule(DemoIdentity.ContractorId, now.AddHours(-1), Guid.NewGuid(), now.AddHours(-18));
        active.Start(DemoIdentity.ContractorId, Guid.NewGuid(), now.AddHours(-1));
        await repository.AddAsync(active, CancellationToken.None);
    }
}

public sealed record CreateRequest(string Title, string Description, WorkOrderPriority Priority);
public sealed record TriageRequest(WorkOrderPriority Priority, int SlaHours = 8);
public sealed record OfferRequest(Guid ContractorId, int ResponseMinutes = 60);
public sealed record ScheduleRequest(DateTimeOffset ScheduledForUtc);
public sealed record ReasonRequest(string Reason);
