using System.Net;
using System.Text;
using System.Text.Json;
using MeterMind.AI;
using MeterMind.Application;
using Microsoft.Extensions.Options;
namespace MeterMind.AI.Tests;

public class CopilotTests
{
    [Fact]public async Task InvalidCredentialProducesActionableErrorWithoutEchoingSecret(){
        var c=new ResponsesCopilot(new HttpClient(new RejectedKeyHandler()){BaseAddress=new Uri("https://example.test/")},Options.Create(new OpenAiOptions{ApiKey="never-print-this-key",Model="mock"}),new FakeTools());
        var error=await Assert.ThrowsAsync<InvalidOperationException>(async()=>{await foreach(var evt in c.Stream("List offline meters")){};});Assert.Contains("401",error.Message);Assert.DoesNotContain("never-print-this-key",error.Message);
    }
    sealed class RejectedKeyHandler:HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized){Content=new StringContent("untrusted error body never-print-this-key")});}
    [Fact] public async Task MissingCredentialsFailsExplicitly() { var c = new ResponsesCopilot(new HttpClient(), Options.Create(new OpenAiOptions()), new FakeTools()); await Assert.ThrowsAsync<InvalidOperationException>(async () => { await foreach (var e in c.Stream("Show network summary")) { }; }); }
    [Fact] public async Task ResponsesFunctionLoopUsesToolEvidenceAndStreamsFinalAnswer() { var handler = new FakeHandler(); var tools = new FakeTools(); var c = new ResponsesCopilot(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") }, Options.Create(new OpenAiOptions { ApiKey = "test-key", Model = "mock-tool-model" }), tools); var events = new List<CopilotEvent>(); await foreach (var evt in c.Stream("Show summary")) events.Add(evt); Assert.Equal(1, tools.Calls); Assert.Contains(events, e => e.Type == "tool"); Assert.Contains(events, e => e.Type == "delta"); Assert.Contains(events, e => e.Type == "done"); Assert.Contains("actual_count", handler.LastBody); }
    [Fact] public async Task UnsupportedToolFailsWithoutExecution() { var tools = new MeterMind.Infrastructure.ToolDispatcher(null!, null!, null!, new MeterMind.Persistence.FixedTenant(Guid.NewGuid()), new ToolCallContext()); await Assert.ThrowsAsync<ArgumentException>(() => tools.Execute("execute_sql", JsonSerializer.Deserialize<JsonElement>("{}"))); await Assert.ThrowsAsync<ArgumentException>(() => tools.Execute("search_meters", JsonSerializer.Deserialize<JsonElement>("{\"utilityId\":\"forbidden\"}"))); }
    sealed class FakeTools : IToolDispatcher { public int Calls; public IReadOnlyList<string> Names => ["get_network_summary"]; public Task<object> Execute(string name, JsonElement args, CancellationToken ct = default) { if (!Names.Contains(name)) throw new ArgumentException(); Calls++; return Task.FromResult<object>(new { actual_count = 300, source = "mock database boundary", queryTime = "2026-11-27T00:00:00Z" }); } }
    sealed class FakeHandler : HttpMessageHandler { int calls; public string LastBody = ""; protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) { LastBody = await request.Content!.ReadAsStringAsync(ct); calls++; var s = calls == 1 ? "data: {\"type\":\"response.output_item.done\",\"item\":{\"type\":\"function_call\",\"call_id\":\"call_1\",\"name\":\"get_network_summary\",\"arguments\":\"{}\"}}\n\ndata: {\"type\":\"response.completed\"}\n\n" : "data: {\"type\":\"response.output_text.delta\",\"delta\":\"300 simulated meters, from get_network_summary.\"}\n\ndata: {\"type\":\"response.completed\"}\n\n"; return new(HttpStatusCode.OK) { Content = new StringContent(s, Encoding.UTF8, "text/event-stream") }; } }
}
