using System.Security.Claims;
using System.Text;
using MeterMind.AI;
using MeterMind.Application;
using MeterMind.Domain;
using MeterMind.Persistence;
using MeterMind.Reporting;
using MeterMind.Simulator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.AspNetCore;

namespace MeterMind.Infrastructure;

public sealed class ClaimsTenant(IHttpContextAccessor accessor) : ITenantContext
{
    ClaimsPrincipal Principal => accessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
    public Guid UtilityId => Guid.TryParse(Principal.FindFirstValue("utility_id"), out var id) ? id : throw new UnauthorizedAccessException("utility_id claim required");
    public string UserId => Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal.FindFirstValue("sub") ?? throw new UnauthorizedAccessException();
    public bool CanOperate => new[] { "Administrator", "NetworkEngineer", "UtilityOperator" }.Any(Principal.IsInRole);
}
public sealed class DemoSigningKey { public byte[] Bytes { get; } = System.Security.Cryptography.RandomNumberGenerator.GetBytes(64); }
public static class Composition
{
    public static IServiceCollection AddMeterMind(this IServiceCollection s, IConfiguration config, bool development, Guid? workerUtility = null)
    {
        s.AddHttpContextAccessor();
        if (workerUtility is Guid u) s.AddScoped<ITenantContext>(_ => new FixedTenant(u)); else s.AddScoped<ITenantContext, ClaimsTenant>();
        var conn = config.GetConnectionString("MeterDb") ?? throw new InvalidOperationException("ConnectionStrings__MeterDb is required.");
        s.AddDbContext<MeterDb>(o => o.UseNpgsql(conn));
        s.AddOptions<HealthThresholds>().Bind(config.GetSection("Health")).Validate(t => t.WeakRssi is >= -200 and <= 0 && t.MinPdr is >= 0 and <= 1 && t.StaleMinutes > 0 && t.OfflineMinutes > t.StaleMinutes, "Invalid health thresholds").ValidateOnStart();
        s.AddOptions<SimulationOptions>().Bind(config.GetSection("Simulation")).Validate(o => o.RadiusM > 0 && o.RadiusM <= 10000 && o.Days is >= 1 and <= 366 && Math.Abs(o.Latitude) <= 85 && Math.Abs(o.Longitude) <= 180 && o.End.Offset == TimeSpan.Zero, "Invalid simulation configuration").ValidateOnStart();
        s.AddScoped<INetworkService, NetworkService>(); s.AddScoped<IReportService, ReportService>(); s.AddScoped<ReportProcessor>(); s.AddScoped<HistoricalSeeder>(); s.AddScoped<SimulationService>();
        s.AddScoped<ToolCallContext>(); s.AddScoped<CanonicalIngestion>(); s.AddScoped<IToolDispatcher, ToolDispatcher>(); s.AddHttpClient<ResponsesCopilot>(c => { c.BaseAddress = new Uri("https://api.openai.com/v1/"); c.Timeout = TimeSpan.FromSeconds(120); });
        s.AddOptions<OpenAiOptions>().Bind(config.GetSection("OpenAI")).Validate(o => o.MaxToolRounds is >= 1 and <= 8 && o.MaxOutputTokens is >= 200 and <= 4000, "Invalid OpenAI usage limits").ValidateOnStart(); s.AddScoped<IDlmsAdapter, SimulatedDlmsAdapter>();
        s.AddOpenTelemetry().ConfigureResource(r => r.AddService("MeterMind")).WithTracing(t => { t.AddAspNetCoreInstrumentation(); if (!string.IsNullOrEmpty(config["OTEL_EXPORTER_OTLP_ENDPOINT"])) t.AddOtlpExporter(); });
        if (!string.IsNullOrWhiteSpace(config["APPLICATIONINSIGHTS_CONNECTION_STRING"])) s.AddOpenTelemetry().UseAzureMonitor(o => o.ConnectionString = config["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
        // Generic worker hosts have a fixed trusted utility and no HTTP authorization pipeline.
        if(workerUtility is not null)return s;
        if (development && config.GetValue<bool>("DemoAuth"))
        {
            var key = new DemoSigningKey(); s.AddSingleton(key);
            s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new() { ValidateIssuer = true, ValidIssuer = "metermind-local", ValidateAudience = true, ValidAudience = "metermind", ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(key.Bytes), ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(10) });
        }
        else
        {
            if (string.IsNullOrWhiteSpace(config["Auth:Authority"]) || string.IsNullOrWhiteSpace(config["Auth:Audience"])) throw new InvalidOperationException("Production requires Auth__Authority and Auth__Audience.");
            s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => { o.Authority = config["Auth:Authority"]; o.Audience = config["Auth:Audience"]; o.RequireHttpsMetadata = true; });
        }
        s.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
        {
            o.Events.OnMessageReceived = ctx => { if (ctx.HttpContext.Request.Path.StartsWithSegments("/hubs/network") && ctx.Request.Query.TryGetValue("access_token", out var token)) ctx.Token = token; return Task.CompletedTask; };
        });
        s.AddAuthorizationBuilder().SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Administrator","NetworkEngineer","UtilityOperator","Technician","Viewer").Build()).AddPolicy("Operate", p => p.RequireRole("Administrator", "NetworkEngineer", "UtilityOperator")).AddPolicy("Admin", p => p.RequireRole("Administrator"));
        return s;
    }
}
