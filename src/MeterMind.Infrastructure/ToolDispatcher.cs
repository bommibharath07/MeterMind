using System.Text.Json;
using MeterMind.AI;
using MeterMind.Application;
using MeterMind.Domain;
using MeterMind.Persistence;
using Microsoft.EntityFrameworkCore;
namespace MeterMind.Infrastructure;

public sealed class ToolDispatcher(INetworkService network, IReportService reports, MeterDb db, ITenantContext tenant, ToolCallContext context) : IToolDispatcher
{
    public IReadOnlyList<string> Names => ["get_network_summary", "search_meters", "get_meter_details", "get_weak_signal_meters", "get_offline_meters", "get_meter_neighbors", "get_mesh_topology", "get_routing_history", "get_dlms_profile", "get_meter_events", "compare_meter_health", "get_gateway_health", "find_affected_nodes", "diagnose_meter_failure", "predict_network_risk", "generate_excel_report", "generate_csv_report", "generate_text_report", "propose_work_order"];
    public async Task<object> Execute(string name, JsonElement args, CancellationToken ct = default)
    {
        if (!Names.Contains(name)) throw new ArgumentException("Unknown tool.");
        if (args.ValueKind != JsonValueKind.Object) throw new ArgumentException("Arguments must be an object.");
        foreach (var property in args.EnumerateObject()) if (!ToolCatalog.AllowedArguments.Contains(property.Name)) throw new ArgumentException($"Unsupported argument {property.Name}.");
        string? S(string key) => args.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null;
        string Required(string key) => S(key) ?? throw new ArgumentException($"{key} required.");
        double? N(string key) => args.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetDouble() : null;
        Guid? G(string key) => S(key) is string s ? Guid.TryParse(s, out var g) ? g : throw new ArgumentException($"Invalid {key}.") : null;
        var clock = await db.Simulations.Select(s => (DateTimeOffset?)s.Clock).SingleOrDefaultAsync(ct) ?? DateTimeOffset.UtcNow;
        DateTimeOffset D(string key, DateTimeOffset fallback) => S(key) is string s ? DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var d) ? d.ToUniversalTime() : throw new ArgumentException($"Invalid {key}.") : fallback;
        int limit = args.TryGetProperty("limit", out var lim) ? lim.GetInt32() : 50; if (limit is < 1 or > 100) throw new ArgumentException("Tool limit must be 1–100.");
        var from = D("from", clock.AddDays(-7)); var to = D("to", clock); var filter = new MeterFilter(S("search"), N("rssiBelow"), S("status"), G("sectorId"), G("gatewayId"), limit);
        object result = name switch
        {
            "get_network_summary" => await network.Summary(ct),
            "search_meters" => await network.Search(filter, ct),
            "get_weak_signal_meters" => await network.Search(filter with { RssiBelow = N("rssiBelow") ?? -90 }, ct),
            "get_offline_meters" => await network.Search(filter with { Status = "offline" }, ct),
            "get_meter_details" => await network.Details(Required("meterId"), ct),
            "get_meter_neighbors" => await network.Neighbors(Required("meterId"), ct),
            "get_mesh_topology" => await Mesh(limit, ct),
            "get_routing_history" => await network.Routes(Required("meterId"), from, to, ct),
            "get_dlms_profile" => await network.Profiles(Required("meterId"), Required("profile"), from, to, limit, ct),
            "get_meter_events" => await network.Events(Required("meterId"), from, to, ct),
            "compare_meter_health" => new { First = await network.Details(Required("meterId"), ct), Second = await network.Details(Required("otherMeterId"), ct) },
            "get_gateway_health" => await network.Gateways(ct),
            "find_affected_nodes" => await network.Affected(G("nodeId") ?? throw new ArgumentException("nodeId required"), ct),
            "diagnose_meter_failure" or "predict_network_risk" => await network.Diagnose(Required("meterId"), ct),
            "generate_excel_report" or "generate_csv_report" or "generate_text_report" => new { ReportId = await reports.Queue(new(name == "generate_excel_report" ? "xlsx" : name == "generate_csv_report" ? "csv" : "txt", S("profile") is null ? "inventory" : "profiles", filter, S("meterId"), S("profile"), from, to), ct), Status = "queued", Download = "Use authorized /api/reports/{reportId}/download after completion." },
            "propose_work_order" => await Propose(S("meterId"), Required("description"), ct),
            _ => throw new ArgumentException("Unknown tool.")
        };
        // Audit excludes complete exported datasets; result cards stay bounded.
        var auditId = Guid.NewGuid(); var node = JsonSerializer.SerializeToNode(result, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var json = node.ToJsonString(); db.Add(new ToolExecution { Id = auditId, UtilityId = tenant.UtilityId, ConversationId = context.ConversationId, Tool = name, Arguments = args.GetRawText(), Result = json.Length <= 30000 ? json : JsonSerializer.Serialize(new { sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))), size = json.Length }), Timestamp = DateTimeOffset.UtcNow }); await db.SaveChangesAsync(ct);
        if (node is System.Text.Json.Nodes.JsonObject obj) { obj["evidenceRef"] = auditId.ToString(); return obj; }
        return new { Items = node, EvidenceRef = auditId, QueryTime = DateTimeOffset.UtcNow };
    }
    async Task<object> Mesh(int limit, CancellationToken ct) => new { Items = await db.Meters.OrderBy(m => m.Serial).Select(m => new { m.Serial, m.Id, m.NextHopId, m.SinkId, m.HopCount }).Take(limit).ToListAsync(ct), Total = await db.Meters.CountAsync(ct) };
    async Task<object> Propose(string? id, string description, CancellationToken ct) { if (!tenant.CanOperate) throw new UnauthorizedAccessException(); if (description.Length is < 5 or > 2000) throw new ArgumentException("Description must be 5–2000 characters."); var m = id is null ? null : (await network.Details(id, ct)).Meter; var w = new WorkOrder { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, MeterId = m?.Id, Description = description }; db.Add(w); await db.SaveChangesAsync(ct); return new { w.Id, w.Status, HumanApprovalRequired = true, Dispatched = false }; }
}
