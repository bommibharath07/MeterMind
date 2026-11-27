using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using MeterMind.AI;
using MeterMind.Application;
using MeterMind.Domain;
using MeterMind.Infrastructure;
using MeterMind.Persistence;
using MeterMind.Reporting;
using MeterMind.Simulator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace MeterMind.Integration.Tests;

public sealed class LiveAiFactAttribute : FactAttribute {
    public LiveAiFactAttribute(){if(Environment.GetEnvironmentVariable("METERMIND_RUN_LIVE_AI")!="1")Skip="Live OpenAI evaluation requires explicit opt-in.";}
}
public class LiveCopilotTests {
    [LiveAiFact] public async Task OfflineMeterConversationExportsExactlyMatchingExcelRows(){
        var key=Environment.GetEnvironmentVariable("OPENAI_API_KEY")??throw new InvalidOperationException("OPENAI_API_KEY required.");
        var connection=Environment.GetEnvironmentVariable("METERMIND_TEST_CONNECTION")??throw new InvalidOperationException("Isolated test database required.");
        if(!new Npgsql.NpgsqlConnectionStringBuilder(connection).Database!.EndsWith("_test"))throw new InvalidOperationException("Use an isolated _test database.");
        var tenant=new FixedTenant(Demo.UtilityId);
        await using var db=new MeterDb(new DbContextOptionsBuilder<MeterDb>().UseNpgsql(connection).Options,tenant);
        await db.Database.MigrateAsync();if(!await db.Meters.AnyAsync())await new HistoricalSeeder(db).Seed(new SimulationOptions{Days=1});
        var network=new NetworkService(db,Options.Create(new HealthThresholds()));
        var expected=(await network.Search(new MeterFilter(Status:"offline"))).Items.Select(x=>x.Meter.Serial).Order().ToArray();Assert.NotEmpty(expected);
        var reports=new ReportService(db,tenant);var tools=new ToolDispatcher(network,reports,db,tenant,new ToolCallContext());
        var model=Environment.GetEnvironmentVariable("OPENAI_MODEL");if(string.IsNullOrWhiteSpace(model))model="gpt-4.1-mini";
        var copilot=new ResponsesCopilot(new HttpClient{BaseAddress=new Uri("https://api.openai.com/v1/"),Timeout=TimeSpan.FromSeconds(120)},Options.Create(new OpenAiOptions{ApiKey=key,Model=model,MaxToolRounds=3,MaxOutputTokens=1000}),tools);
        var usedTools=new List<string>();var question="List meters that are offline. Show the exact total and all their serial numbers.";var answer=new StringBuilder();
        await foreach(var evt in copilot.Stream(question)){
            var data=JsonSerializer.SerializeToElement(evt.Data,new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if(evt.Type=="delta")answer.Append(data.GetProperty("text").GetString());if(evt.Type=="tool")usedTools.Add(data.GetProperty("name").GetString()!);
        }
        var listed=Regex.Matches(answer.ToString(),@"\bMM-\d{4}\b").Select(m=>m.Value).Distinct().Order().ToArray();Assert.Equal(expected,listed);
        var history=new List<JsonNode>{JsonSerializer.SerializeToNode(new{role="user",content=question})!,JsonSerializer.SerializeToNode(new{role="assistant",content=answer.ToString()})!};Guid? reportId=null;
        await foreach(var evt in copilot.Stream("I want those offline meters in Excel. Export the complete matching list.",history)){
            if(evt.Type!="tool")continue;var data=JsonSerializer.SerializeToElement(evt.Data,new JsonSerializerOptions(JsonSerializerDefaults.Web));var name=data.GetProperty("name").GetString()!;usedTools.Add(name);
            if(name=="generate_excel_report")reportId=data.GetProperty("result").GetProperty("reportId").GetGuid();
        }
        Assert.NotNull(reportId);var job=await db.Set<GeneratedReport>().SingleAsync(r=>r.Id==reportId);var request=JsonSerializer.Deserialize<ReportRequest>(job.Filter)!;Assert.Equal("offline",request.Filter.Status);
        await new ReportProcessor(db,network).Process();var file=await reports.Download(reportId.Value);
        using var workbook=new XLWorkbook(new MemoryStream(file.Content));var exported=workbook.Worksheet(1).RowsUsed().Skip(2).Select(r=>r.Cell(1).GetString()).Order().ToArray();Assert.Equal(expected,exported);
        var folder=Environment.GetEnvironmentVariable("METERMIND_LIVE_EVIDENCE_PATH")??"artifacts/live-ai";Directory.CreateDirectory(folder);await File.WriteAllBytesAsync(Path.Combine(folder,"offline-meters.xlsx"),file.Content);
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{model,expectedCount=expected.Length,exportedCount=exported.Length,serials=exported,tools=usedTools,reportId,verified=true},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"LIVE_AI_PASS offline={expected.Length}; excel_rows={exported.Length}; exact_match=true; model={model}");
    }
}
