using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using MeterMind.Application;
using MeterMind.Domain;
using MeterMind.Persistence;
using MeterMind.Reporting;
using MeterMind.Simulator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using MeterMind.Infrastructure;
namespace MeterMind.Integration.Tests;

public sealed class DatabaseFactAttribute : FactAttribute { public DatabaseFactAttribute() { if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("METERMIND_TEST_CONNECTION"))) Skip = "Set METERMIND_TEST_CONNECTION to an isolated PostgreSQL test database."; } }
public class AcceptanceTests
{
    [DatabaseFact]
    public async Task FullDatabaseAcceptance()
    {
        var conn = Environment.GetEnvironmentVariable("METERMIND_TEST_CONNECTION")!; if (!new Npgsql.NpgsqlConnectionStringBuilder(conn).Database!.EndsWith("_test")) throw new InvalidOperationException("Acceptance tests require a database ending in _test.");
        var opts = new DbContextOptionsBuilder<MeterDb>().UseNpgsql(conn).Options; var tenant = new FixedTenant(Demo.UtilityId);
        await using var db = new MeterDb(opts, tenant); await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
        var days = Environment.GetEnvironmentVariable("METERMIND_FULL_SEED") == "1" ? 30 : 1; var simulation = new SimulationOptions { Days = days }; var watch = Stopwatch.StartNew(); await new HistoricalSeeder(db).Seed(simulation); var seedSeconds = watch.Elapsed.TotalSeconds;
        Assert.Equal(300, await db.Meters.CountAsync()); var expected = 300L * (288 * days + 96 * days + 48 * days + days + 1); Assert.Equal(expected, await db.Telemetry.LongCountAsync() + await db.Readings.LongCountAsync());
        await new HistoricalSeeder(db).Seed(simulation); Assert.Equal(expected, await db.Telemetry.LongCountAsync() + await db.Readings.LongCountAsync());
        var network = new NetworkService(db, Options.Create(new HealthThresholds())); var weak = await network.Search(new(RssiBelow: -90)); var actual = await db.Meters.Where(m => m.Rssi < -90).OrderBy(m => m.Serial).Select(m => m.Serial).ToListAsync(); Assert.Equal(actual, weak.Items.Select(x => x.Meter.Serial));
        foreach (var profile in Demo.Profiles) { var json = JsonSerializer.Serialize(await network.Profiles("MM-0012", profile, simulation.End.AddDays(-days), simulation.End)); Assert.Contains("Timestamp", json); }
        var reports = new ReportService(db, tenant); var report = await reports.Queue(new("xlsx", "inventory", new(RssiBelow: -90))); await new ReportProcessor(db, network).Process(); var file = await reports.Download(report); using var workbook = new XLWorkbook(new MemoryStream(file.Content)); Assert.Equal(actual, workbook.Worksheet(1).RowsUsed().Skip(2).Select(r => r.Cell(1).GetString()));
        var otherId = Guid.NewGuid(); db.Utilities.Add(new() { Id = otherId, Name = "Other utility" }); await db.SaveChangesAsync(); await using var other = new MeterDb(opts, new FixedTenant(otherId)); Assert.Empty(await other.Meters.ToListAsync()); await Assert.ThrowsAsync<KeyNotFoundException>(() => new NetworkService(other, Options.Create(new HealthThresholds())).Details("MM-0012")); await Assert.ThrowsAsync<KeyNotFoundException>(() => new ReportService(other, new FixedTenant(otherId)).Download(report));
        await using var factory = new ApiFactory(conn); var client = factory.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/meters")).StatusCode); var auth = await (await client.PostAsync("/auth/demo", null)).Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("token").GetString()); var response = await client.GetAsync("/api/meters?RssiBelow=-90"); response.EnsureSuccessStatusCode(); var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(actual.Count, jsonResult.GetProperty("total").GetInt32()); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        var aiStatus=await client.GetFromJsonAsync<JsonElement>("/api/copilot/status");Assert.False(aiStatus.GetProperty("configured").GetBoolean());
        var conversationsBefore=await db.Set<Conversation>().CountAsync();var pausedChat=await client.PostAsJsonAsync("/api/copilot/chat",new{message="List offline meters"});Assert.Equal(HttpStatusCode.ServiceUnavailable,pausedChat.StatusCode);
        var pausedProblem=await pausedChat.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal("AI chat is not enabled",pausedProblem.GetProperty("title").GetString());Assert.Equal(conversationsBefore,await db.Set<Conversation>().CountAsync());
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/meters?Status=offline")).StatusCode);
        watch.Restart(); await network.Summary(); var summaryMs = watch.Elapsed.TotalMilliseconds;
        await using(var transaction=await db.Database.BeginTransactionAsync()){
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Simulation:Enabled","true"}}).Build();
            var service=new SimulationService(db,tenant,config,Options.Create(simulation));
            await service.Scenario("MM-0012","congestion");await service.Scenario("MM-0012","congestion");
            Assert.Contains((await network.Diagnose("MM-0012")).Findings,f=>f.Rule=="congestion");
            await service.SetRunning(true);var before=await db.Telemetry.LongCountAsync();await service.Tick();Assert.Equal(before+300,await db.Telemetry.LongCountAsync());
            await service.SetRunning(false);await service.Tick();Assert.Equal(before+300,await db.Telemetry.LongCountAsync());await transaction.RollbackAsync();db.ChangeTracker.Clear();
        }
        Console.WriteLine($"ACCEPTANCE baseline={expected}; seed_seconds={seedSeconds:F2}; summary_ms={summaryMs:F2}; weak_records={actual.Count}; profiles=10; idempotency=true; isolation=true; export_exact=true; scenarios=true; continuous_tick=true");
    }
    sealed class ApiFactory(string conn) : WebApplicationFactory<MeterMind.API.ApiMarker> { protected override void ConfigureWebHost(IWebHostBuilder builder) { builder.UseEnvironment("Development"); builder.UseSetting("ConnectionStrings:MeterDb", conn); builder.UseSetting("DemoAuth", "true"); builder.UseSetting("Simulation:Enabled", "true"); builder.UseSetting("OpenAI:ApiKey", ""); builder.UseSetting("OpenAI:Model", ""); } }
}
