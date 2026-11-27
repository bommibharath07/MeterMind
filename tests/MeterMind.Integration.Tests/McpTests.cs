using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MeterMind.Domain;
using MeterMind.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MeterMind.Persistence;
using Microsoft.EntityFrameworkCore;
namespace MeterMind.Integration.Tests;

public class McpTests
{
    [DatabaseFact]
    public async Task McpRejectsAnonymousAndExecutesUtilityScopedTools()
    {
        if(!new Npgsql.NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("METERMIND_TEST_CONNECTION")).Database!.EndsWith("_test"))throw new InvalidOperationException("MCP acceptance requires an isolated _test database.");
        await using var factory = new Factory(); var client = factory.CreateClient(); client.DefaultRequestHeaders.Accept.Add(new("application/json")); client.DefaultRequestHeaders.Accept.Add(new("text/event-stream"));
        var metadata=new Dictionary<string,object>{{"io.modelcontextprotocol/protocolVersion","2026-07-28"},{"io.modelcontextprotocol/clientCapabilities",new{}},{"io.modelcontextprotocol/clientInfo",new{name="MeterMind acceptance",version="1.0"}}};
        var message = new { jsonrpc = "2.0", id = 1, method = "tools/list", @params = new { _meta=metadata } };
        client.DefaultRequestHeaders.Add("Origin","https://untrusted.example");Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/mcp",message)).StatusCode);client.DefaultRequestHeaders.Remove("Origin");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/mcp", message)).StatusCode);
        string Token(Guid utility) { var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "mcp-test"), new Claim("utility_id", utility.ToString()), new Claim(ClaimTypes.Role, "Viewer") }; return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("metermind-local", "metermind", claims, expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(factory.Services.GetRequiredService<DemoSigningKey>().Bytes), SecurityAlgorithms.HmacSha256))); }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(Demo.UtilityId));
        client.DefaultRequestHeaders.Add("MCP-Protocol-Version", "2026-07-28");
        client.DefaultRequestHeaders.Add("Mcp-Method","tools/list");
        var response = await client.PostAsJsonAsync("/mcp", message); var text = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode,text); Assert.Contains("get_network_summary", text); Assert.Contains("propose_work_order", text);
        var otherUtility = Guid.NewGuid(); await using (var db = new MeterDb(new DbContextOptionsBuilder<MeterDb>().UseNpgsql(Environment.GetEnvironmentVariable("METERMIND_TEST_CONNECTION")).Options, new FixedTenant(otherUtility))) { await db.Database.MigrateAsync(); db.Utilities.Add(new() { Id = otherUtility, Name = "MCP isolated test utility" }); await db.SaveChangesAsync(); }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(otherUtility));
        client.DefaultRequestHeaders.Remove("Mcp-Method");client.DefaultRequestHeaders.Add("Mcp-Method","tools/call");client.DefaultRequestHeaders.Add("Mcp-Name","search_meters");
        var other = await client.PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 2, method = "tools/call", @params = new { _meta=metadata, name = "search_meters", arguments = new { argumentsJson = "{}" } } });var body=await other.Content.ReadAsStringAsync(); Assert.True(other.IsSuccessStatusCode,body); Assert.DoesNotContain("MM-0012", body); Assert.Contains("total", body); Assert.DoesNotContain("\"isError\":true", body);
    }
    sealed class Factory : WebApplicationFactory<MeterMind.McpServer.McpMarker> { protected override void ConfigureWebHost(IWebHostBuilder builder) { builder.UseEnvironment("Development"); builder.UseSetting("ConnectionStrings:MeterDb", Environment.GetEnvironmentVariable("METERMIND_TEST_CONNECTION")); builder.UseSetting("DemoAuth", "true"); } }
}
