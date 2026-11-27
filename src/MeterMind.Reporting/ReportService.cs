using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using MeterMind.Application;
using MeterMind.Domain;
using MeterMind.Persistence;
using Microsoft.EntityFrameworkCore;
namespace MeterMind.Reporting;

public sealed class ReportService(MeterDb db, ITenantContext tenant) : IReportService
{
    public async Task<Guid> Queue(ReportRequest request, CancellationToken ct = default)
    {
        if (!await db.Utilities.AnyAsync(ct)) throw new InvalidOperationException("This utility has not been provisioned. Finish seeding before requesting reports.");
        if (request.Format is not ("csv" or "xlsx" or "txt") || request.Kind is not ("inventory" or "weak-signal" or "offline" or "diagnostics" or "profiles" or "incidents")) throw new ArgumentException("Unsupported report format or kind.");
        if (request.Kind == "profiles" && (request.MeterId is null || request.Profile is null || request.From is null || request.To is null)) throw new ArgumentException("Profile report needs meter, profile, from and to.");
        if (await db.Set<GeneratedReport>().CountAsync(r => r.Status == "queued" || r.Status == "processing", ct) >= 10) throw new ArgumentException("Report queue limit reached.");
        var r = new GeneratedReport { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, Owner = tenant.UserId, Format = request.Format, Kind = request.Kind, Filter = JsonSerializer.Serialize(request), Timestamp = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        db.Add(r); db.Add(new AuditLog { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, Actor = tenant.UserId, Action = "report.request", Resource = r.Id.ToString(), Timestamp = DateTimeOffset.UtcNow }); await db.SaveChangesAsync(ct); return r.Id;
    }
    public async Task<object> List(CancellationToken ct = default) => await db.Set<GeneratedReport>().Where(r => r.Owner == tenant.UserId).OrderByDescending(r => r.Timestamp).Select(r => new { r.Id, r.Kind, r.Format, r.Status, r.Timestamp, r.ExpiresAt, r.Error }).Take(100).ToListAsync(ct);
    public async Task<(byte[] Content, string Mime, string Name)> Download(Guid id, CancellationToken ct = default)
    {
        var r = await db.Set<GeneratedReport>().SingleOrDefaultAsync(r => r.Id == id && r.Owner == tenant.UserId, ct) ?? throw new KeyNotFoundException();
        if (r.ExpiresAt <= DateTimeOffset.UtcNow) throw new ArgumentException("Report expired."); if (r.Status != "complete" || r.Content is null) throw new ArgumentException("Report is not ready.");
        return (r.Content, r.Format == "xlsx" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : r.Format == "csv" ? "text/csv" : "text/plain", $"metermind-{r.Kind}-{r.Id}.{r.Format}");
    }
    public static string SafeCell(string? s) { s ??= ""; return s.TrimStart().StartsWith('=') || s.TrimStart().StartsWith('+') || s.TrimStart().StartsWith('-') || s.TrimStart().StartsWith('@') || s.StartsWith('\t') || s.StartsWith('\r') ? "'" + s : s; }
    public static byte[] Render(string format, IReadOnlyList<string> headers, IReadOnlyList<string[]> rows, DateTimeOffset timestamp)
    {
        if (format == "xlsx")
        {
            using var book = new XLWorkbook(); var sheet = book.Worksheets.Add("MeterMind");
            sheet.Cell(1, 1).Value = $"Simulation data • Generated {timestamp:O}";
            for (int c = 0; c < headers.Count; c++) sheet.Cell(3, c + 1).Value = headers[c];
            for (int r = 0; r < rows.Count; r++) for (int c = 0; c < headers.Count; c++) sheet.Cell(r + 4, c + 1).Value = SafeCell(rows[r][c]);
            if (rows.Count > 0) sheet.Range(3, 1, 3 + rows.Count, headers.Count).CreateTable("MeterMindReport");
            sheet.SheetView.FreezeRows(3);
            // Deterministic widths avoid depending on desktop fonts in Linux containers.
            for(int c=0;c<headers.Count;c++)sheet.Column(c+1).Width=Math.Clamp(headers[c].Length+4,14,48);
            using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
        }
        static string Escape(string s) => "\"" + SafeCell(s).Replace("\"", "\"\"") + "\"";
        var text = new StringBuilder(); if (format == "txt") text.AppendLine($"MeterMind • Simulation • {timestamp:O}");
        text.AppendLine(string.Join(format == "csv" ? "," : "\t", headers.Select(format == "csv" ? Escape : (Func<string, string>)SafeCell)));
        foreach (var row in rows) text.AppendLine(string.Join(format == "csv" ? "," : "\t", row.Select(format == "csv" ? Escape : (Func<string, string>)SafeCell)));
        return Encoding.UTF8.GetBytes(text.ToString());
    }
}
public sealed class ReportProcessor(MeterDb db, INetworkService network)
{
    public async Task<int> Process(CancellationToken ct = default)
    {
        // SKIP LOCKED makes multiple workers safe. The transaction also returns a failed worker's claim to the queue.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var jobs = await db.Set<GeneratedReport>().FromSqlInterpolated($"SELECT * FROM \"GeneratedReport\" WHERE \"UtilityId\"={db.UtilityId} AND \"Status\"='queued' ORDER BY \"Timestamp\" LIMIT 1 FOR UPDATE SKIP LOCKED").ToListAsync(ct);
        foreach (var job in jobs)
        {
            job.Status = "processing";
            try
            {
                var request = JsonSerializer.Deserialize<ReportRequest>(job.Filter)!;
                var headers = new List<string>(); var rows = new List<string[]>();
                if (job.Kind == "profiles")
                {
                    var meter = (await network.Details(request.MeterId!, ct)).Meter; NetworkService.Range(request.From!.Value, request.To!.Value);
                    if (request.Profile!.EndsWith("events"))
                    {
                        headers.AddRange(["Meter", "Timestamp UTC", "Code", "Description"]);
                        var q = db.Set<MeterEvent>().Where(x => x.MeterId == meter.Id && x.Profile == request.Profile && x.Timestamp >= request.From && x.Timestamp <= request.To).OrderBy(x => x.Timestamp);
                        if (await q.CountAsync(ct) > 100000) throw new ArgumentException("Narrow export to 100,000 rows.");
                        await foreach (var x in q.AsAsyncEnumerable().WithCancellation(ct)) rows.Add([meter.Serial, x.Timestamp.ToString("O"), x.Code, x.Description]);
                    }
                    else
                    {
                        headers.AddRange(["Meter", "Profile", "Timestamp UTC", "Voltage V", "Current A", "Active power kW", "Cumulative kWh", "Interval kWh", "Quality"]);
                        var q = db.Readings.Where(x => x.MeterId == meter.Id && x.Profile == request.Profile && x.Timestamp >= request.From && x.Timestamp <= request.To).OrderBy(x => x.Timestamp);
                        if (await q.CountAsync(ct) > 100000) throw new ArgumentException("Narrow export to 100,000 rows.");
                        await foreach (var x in q.AsAsyncEnumerable().WithCancellation(ct)) rows.Add([meter.Serial, x.Profile, x.Timestamp.ToString("O"), F(x.Voltage), F(x.CurrentA), F(x.ActivePowerKw), F(x.CumulativeKwh), F(x.IntervalKwh), x.Quality]);
                    }
                }
                else if (job.Kind == "incidents")
                {
                    headers.AddRange(["ID", "Title", "Severity", "Status", "Timestamp UTC"]);
                    foreach (var x in await db.Set<Incident>().OrderBy(x => x.Timestamp).Take(100000).ToListAsync(ct)) rows.Add([x.Id.ToString(), x.Title, x.Severity, x.Status, x.Timestamp.ToString("O")]);
                }
                else
                {
                    headers.AddRange(["Serial", "Status", "RSSI dBm", "PDR fraction", "Read success fraction", "Hops", "Latitude degrees", "Longitude degrees", "Last communication UTC"]);
                    if (job.Kind == "diagnostics") headers.Add("Diagnostic findings");
                    var f = request.Filter with { RssiBelow = job.Kind == "weak-signal" ? -90 : request.Filter.RssiBelow, Status = job.Kind == "offline" ? "offline" : request.Filter.Status, Limit = 1000, Offset = 0 };
                    while (true)
                    {
                        var page = await network.Search(f, ct);
                        foreach (var x in page.Items) { var m = x.Meter; var row = new List<string> { m.Serial, x.Status, F(m.Rssi), F(m.Pdr), F(m.ReadSuccess), m.HopCount?.ToString() ?? "", F(m.Latitude), F(m.Longitude), m.LastCommunication?.ToString("O") ?? "" }; if (job.Kind == "diagnostics") row.Add(string.Join("; ", (await network.Diagnose(m.Serial, ct)).Findings.Select(d => $"{d.Rule}: {d.Conclusion}"))); rows.Add(row.ToArray()); }
                        if (rows.Count >= page.Total) break; if (rows.Count > 100000) throw new ArgumentException("Narrow export to 100,000 rows."); f = f with { Offset = f.Offset + f.Limit };
                    }
                }
                job.Content = ReportService.Render(job.Format, headers, rows, job.Timestamp); job.Status = "complete";
            }
            catch (Exception e) when (e is not OperationCanceledException) { job.Status = "failed"; job.Error = e is ArgumentException ? e.Message : "Report generation failed; inspect server logs."; }
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct); return jobs.Count;
    }
    static string F(double? x) => x?.ToString("G12", CultureInfo.InvariantCulture) ?? "";
}
