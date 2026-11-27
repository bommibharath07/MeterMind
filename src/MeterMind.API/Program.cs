using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.RateLimiting;
using MeterMind.AI;
using MeterMind.API;
using MeterMind.Application;
using MeterMind.Domain;
using MeterMind.Infrastructure;
using MeterMind.Persistence;
using MeterMind.Reporting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMeterMind(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddProblemDetails(); builder.Services.AddSignalR(); builder.Services.AddHealthChecks(); builder.Services.AddHostedService<NetworkUpdatePump>();
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("api", c => RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirstValue("utility_id") ?? c.Connection.RemoteIpAddress?.ToString() ?? "anonymous", _ => new() { PermitLimit = 180, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); o.AddPolicy("ai", c => RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous", _ => new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) })); });
if (builder.Environment.IsDevelopment()) builder.Services.AddHostedService<DemoReportPump>();
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async ctx =>
{
    var e = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    ctx.Response.StatusCode = e switch { KeyNotFoundException => 404, ArgumentException => 400, UnauthorizedAccessException => 403, InvalidOperationException => 409, _ => 500 };
    await Results.Problem(statusCode: ctx.Response.StatusCode, title: ctx.Response.StatusCode == 500 ? "Request failed" : e?.Message).ExecuteAsync(ctx);
}));
app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/auth/config", () => new { DemoEnabled = app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DemoAuth"), Authority = builder.Configuration["Auth:Authority"], Audience = builder.Configuration["Auth:Audience"] });
app.MapGet("/health/ready", async (MeterDb db, CancellationToken ct) => await db.Database.CanConnectAsync(ct) && !(await db.Database.GetPendingMigrationsAsync(ct)).Any() ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DemoAuth")) app.MapPost("/auth/demo", (DemoSigningKey key) =>
{
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "local-demo"), new Claim("utility_id", Demo.UtilityId.ToString()), new Claim(ClaimTypes.Role, "Administrator") };
    var token = new JwtSecurityToken("metermind-local", "metermind", claims, expires: DateTime.UtcNow.AddHours(2), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key.Bytes), SecurityAlgorithms.HmacSha256));
    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), expiresIn = 7200, mode = "local simulation" });
}).RequireRateLimiting("api");
var api = app.MapGroup("/api").RequireAuthorization().RequireRateLimiting("api");
api.MapGet("/network/summary", (INetworkService n, CancellationToken ct) => n.Summary(ct));
api.MapGet("/meters", ([AsParameters] MeterFilter filter, INetworkService n, CancellationToken ct) => n.Search(filter, ct));
api.MapGet("/meters/{id}", (string id, INetworkService n, CancellationToken ct) => n.Details(id, ct));
api.MapGet("/network/topology", (DateTimeOffset? at, INetworkService n, CancellationToken ct) => n.Topology(at, ct));
api.MapGet("/meters/{id}/neighbors", (string id, INetworkService n, CancellationToken ct) => n.Neighbors(id, ct));
api.MapGet("/meters/{id}/routes", (string id, DateTimeOffset from, DateTimeOffset to, INetworkService n, CancellationToken ct) => n.Routes(id, from, to, ct));
api.MapGet("/meters/{id}/profiles/{profile}", (string id, string profile, DateTimeOffset from, DateTimeOffset to, int? limit, INetworkService n, CancellationToken ct) => n.Profiles(id, profile, from, to, limit ?? 500, ct));
api.MapGet("/meters/{id}/events", (string id, DateTimeOffset from, DateTimeOffset to, INetworkService n, CancellationToken ct) => n.Events(id, from, to, ct));
api.MapGet("/meters/{id}/diagnosis", (string id, INetworkService n, CancellationToken ct) => n.Diagnose(id, ct));
api.MapGet("/meters/{id}/communication", async (string id, INetworkService n, MeterDb db, CancellationToken ct) => { var m = (await n.Details(id, ct)).Meter; return await db.Set<CommunicationAttempt>().Where(x => x.MeterId == m.Id).OrderByDescending(x => x.Timestamp).Take(200).ToListAsync(ct); });
api.MapGet("/network/affected/{id:guid}", (Guid id, INetworkService n, CancellationToken ct) => n.Affected(id, ct));
api.MapGet("/gateways", (INetworkService n, CancellationToken ct) => n.Gateways(ct));
api.MapGet("/catalog", async (MeterDb db, CancellationToken ct) => new { Profiles = await db.Set<ProfileDefinition>().ToListAsync(ct), Obis = await db.Set<ObisObject>().ToListAsync(ct), Sectors = await db.Set<Sector>().ToListAsync(ct) });
api.MapGet("/incidents", async (MeterDb db, CancellationToken ct) => await db.Set<Incident>().OrderByDescending(x => x.Timestamp).Take(500).ToListAsync(ct));
api.MapPatch("/incidents/{id:guid}", async (Guid id, IncidentStatus body, MeterDb db, ITenantContext tenant, CancellationToken ct) => { if (body.Status is not ("open" or "investigating" or "resolved")) throw new ArgumentException("Invalid incident status"); var incident = await db.Set<Incident>().SingleOrDefaultAsync(i => i.Id == id, ct) ?? throw new KeyNotFoundException(); incident.Status = body.Status; db.Add(new AuditLog { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, Actor = tenant.UserId, Action = "incident." + body.Status, Resource = id.ToString(), Timestamp = DateTimeOffset.UtcNow }); await db.SaveChangesAsync(ct); return Results.Ok(incident); }).RequireAuthorization("Operate");
api.MapGet("/work-orders", async (MeterDb db, CancellationToken ct) => await db.Set<WorkOrder>().Take(500).ToListAsync(ct));
api.MapPost("/work-orders/{id:guid}/approve", async (Guid id, MeterDb db, ITenantContext tenant, CancellationToken ct) => { var w = await db.Set<WorkOrder>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException(); w.HumanApproved = true; w.Status = "approved-for-review"; db.Add(new AuditLog { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, Actor = tenant.UserId, Action = "work-order.approve", Resource = id.ToString(), Timestamp = DateTimeOffset.UtcNow }); await db.SaveChangesAsync(ct); return Results.Ok(new { w.Id, w.Status, Dispatched = false }); }).RequireAuthorization("Admin");
api.MapPost("/reports", async (ReportRequest request, IReportService r, CancellationToken ct) => Results.Accepted(value: new { id = await r.Queue(request, ct) }));
api.MapGet("/reports", (IReportService r, CancellationToken ct) => r.List(ct));
api.MapGet("/reports/{id:guid}/download", async (Guid id, IReportService r, CancellationToken ct) => { var f = await r.Download(id, ct); return Results.File(f.Content, f.Mime, f.Name); });
api.MapGet("/simulation", (SimulationService sim, CancellationToken ct) => sim.Status(ct));
api.MapPost("/simulation/scenarios", (ScenarioRequest request, SimulationService sim, CancellationToken ct) => sim.Scenario(request.MeterSerial, request.Cause, ct)).RequireAuthorization("Operate");
api.MapPost("/simulation/{action}", async (string action, SimulationService sim, CancellationToken ct) => { if (action == "start") await sim.SetRunning(true, ct); else if (action == "stop") await sim.SetRunning(false, ct); else if (action == "reset") await sim.Reset(ct); else throw new ArgumentException("Invalid simulation action"); return Results.Ok(await sim.Status(ct)); }).RequireAuthorization("Operate");
api.MapGet("/settings", (ITenantContext tenant) => new { tenant.UtilityId, tenant.UserId, tenant.CanOperate, Roles = new[] { "Administrator", "NetworkEngineer", "UtilityOperator", "Technician", "Viewer" }, IdentityManagement = "Use your configured identity provider for account and role assignments.", PhysicalWritesEnabled = false });
api.MapGet("/audit", async (MeterDb db, CancellationToken ct) => await db.Set<AuditLog>().OrderByDescending(x => x.Timestamp).Take(200).ToListAsync(ct)).RequireAuthorization("Admin");
api.MapGet("/copilot/status", (Microsoft.Extensions.Options.IOptions<OpenAiOptions> o) => new { Configured = o.Value.IsConfigured, Model = o.Value.Model });
api.MapGet("/conversations", async (MeterDb db, ITenantContext tenant, CancellationToken ct) => await db.Set<Conversation>().Where(c => c.Owner == tenant.UserId).OrderByDescending(c => c.Timestamp).Take(50).ToListAsync(ct));
api.MapPost("/copilot/chat", async (ChatRequest request, ResponsesCopilot copilot, Microsoft.Extensions.Options.IOptions<OpenAiOptions> options, MeterDb db, ITenantContext tenant, ToolCallContext toolContext, HttpContext context, CancellationToken ct) =>
{
    if (!options.Value.IsConfigured) { await Results.Problem(statusCode: 503, title: "AI chat is not enabled", detail: "Network inspection and reports remain available. Configure OpenAI later to enable conversations.").ExecuteAsync(context); return; }
    var conversation = request.ConversationId is Guid id ? await db.Set<Conversation>().SingleOrDefaultAsync(c => c.Id == id && c.Owner == tenant.UserId, ct) ?? throw new KeyNotFoundException() : new Conversation { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, Owner = tenant.UserId, Timestamp = DateTimeOffset.UtcNow };
    if (db.Entry(conversation).State == EntityState.Detached) { db.Add(conversation); await db.SaveChangesAsync(ct); }
    toolContext.ConversationId = conversation.Id;
    var history = JsonSerializer.Deserialize<List<JsonNode>>(conversation.Messages) ?? []; var answer = new StringBuilder();
    context.Response.ContentType = "text/event-stream"; context.Response.Headers.CacheControl = "no-cache";
    try
    {
        await foreach (var evt in copilot.Stream(request.Message, history, ct)) { if (evt.Type == "delta") answer.Append(JsonSerializer.SerializeToNode(evt.Data)!["text"]!.GetValue<string>()); await context.Response.WriteAsync($"event: {evt.Type}\ndata: {JsonSerializer.Serialize(evt.Data, new JsonSerializerOptions(JsonSerializerDefaults.Web))}\n\n", ct); await context.Response.Body.FlushAsync(ct); }
        history.Add(JsonSerializer.SerializeToNode(new { role = "user", content = request.Message })!); history.Add(JsonSerializer.SerializeToNode(new { role = "assistant", content = answer.ToString() })!); conversation.Messages = JsonSerializer.Serialize(history.TakeLast(20));
        if (db.Entry(conversation).State == EntityState.Detached) db.Add(conversation); await db.SaveChangesAsync(ct);
        await context.Response.WriteAsync($"event: conversation\ndata: {{\"id\":\"{conversation.Id}\"}}\n\n", ct);
    }
    catch (Exception e) when (e is not OperationCanceledException) { app.Logger.LogWarning(e, "Copilot request failed"); await context.Response.WriteAsync($"event: error\ndata: {JsonSerializer.Serialize(new { message = e is InvalidOperationException or ArgumentException ? e.Message : "Copilot service unavailable. Check server configuration." })}\n\n", ct); }
}).RequireRateLimiting("ai");
app.MapHub<NetworkHub>("/hubs/network").RequireAuthorization();
app.Run();
public partial class Program;
public record IncidentStatus(string Status);
public record ChatRequest(string Message, Guid? ConversationId = null);
public record ScenarioRequest(string MeterSerial, string Cause);
public sealed class NetworkHub : Hub { public override async Task OnConnectedAsync() { if (!Guid.TryParse(Context.User?.FindFirstValue("utility_id"), out var utility)) throw new HubException("Utility identity required"); await Groups.AddToGroupAsync(Context.ConnectionId, utility.ToString()); await base.OnConnectedAsync(); } }
public sealed class DemoReportPump(IServiceScopeFactory factory, IHubContext<NetworkHub> hub, ILogger<DemoReportPump> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) { while (!stoppingToken.IsCancellationRequested) { try { using var scope = factory.CreateScope(); var config = scope.ServiceProvider.GetRequiredService<IConfiguration>(); await using var db = new MeterDb(new DbContextOptionsBuilder<MeterDb>().UseNpgsql(config.GetConnectionString("MeterDb")).Options, new FixedTenant(Demo.UtilityId)); var network = new NetworkService(db, Microsoft.Extensions.Options.Options.Create(new HealthThresholds())); await new ReportProcessor(db, network).Process(stoppingToken); var clock = await db.Simulations.Select(s => s.Clock).SingleOrDefaultAsync(stoppingToken); await hub.Clients.Group(Demo.UtilityId.ToString()).SendAsync("networkUpdated", new { dataTime = clock }, stoppingToken); } catch (Exception e) when (e is not OperationCanceledException) { logger.LogWarning("Demo background processing unavailable: {ErrorType}", e.GetType().Name); } await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } }
}
