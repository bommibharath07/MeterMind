using MeterMind.Infrastructure;
using ModelContextProtocol.AspNetCore;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMeterMind(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddMcpServer().WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless).WithTools<MeterMind.McpServer.MeterTools>();
var app = builder.Build();
app.Use(async(ctx,next)=>{
    if(ctx.Request.Headers.TryGetValue("Origin",out var origin)){
        var allowed=(builder.Configuration["Mcp:AllowedOrigins"]??(app.Environment.IsDevelopment()?"http://127.0.0.1:4200,http://localhost:4200":"")).Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if(!allowed.Contains(origin.ToString(),StringComparer.Ordinal)){ctx.Response.StatusCode=403;return;}
    }
    await next();
});
app.UseAuthentication(); app.UseAuthorization(); app.MapMcp("/mcp").RequireAuthorization(); app.MapGet("/health/live", () => new { status = "live" }); app.Run();
