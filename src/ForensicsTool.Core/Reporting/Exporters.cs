using System.Text;
using System.Text.Json;
using ForensicsTool.Core.Interfaces;
using ForensicsTool.Core.Models;
using ForensicsTool.Core.Serialization;

namespace ForensicsTool.Core.Reporting;

public sealed class JsonReportExporter : IReportExporter
{
    public async Task ExportAsync(IReadOnlyCollection<FileForensicReport> reports, string outputPath, CancellationToken cancellationToken = default)
    { await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(reports, ReportJsonContext.Default.IReadOnlyCollectionFileForensicReport), cancellationToken); }
}

public sealed class CsvReportExporter : IReportExporter
{
    public Task ExportAsync(IReadOnlyCollection<FileForensicReport> reports, string outputPath, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder("FileName,Format,Size,SHA256,Severity,Anomalies\n");
        foreach (var r in reports) builder.AppendLine(string.Join(',', Quote(r.FileName), r.DetectedFormat, r.Size, r.Sha256, r.HighestSeverity, Quote(string.Join("; ", r.Anomalies.Select(a => a.Code)))));
        return File.WriteAllTextAsync(outputPath, builder.ToString(), cancellationToken);
    }
    private static string Quote(string value) => '"' + value.Replace("\"", "\"\"") + '"';
}

public sealed class HtmlReportExporter : IReportExporter
{
    public Task ExportAsync(IReadOnlyCollection<FileForensicReport> reports, string outputPath, CancellationToken cancellationToken = default)
    {
        var rows = string.Join("\n", reports.Select(r => $"<tr><td>{Html(r.FileName)}</td><td>{r.DetectedFormat}</td><td>{r.Size}</td><td><code>{r.Sha256}</code></td><td>{r.HighestSeverity}</td><td>{Html(string.Join(", ", r.Anomalies.Select(a => a.Code)))}</td></tr>"));
        var html = $"<!doctype html><meta charset=\"utf-8\"><title>ForensicsTool Report</title><style>body{{font:14px system-ui;margin:2rem}}table{{border-collapse:collapse;width:100%}}td,th{{border:1px solid #ccc;padding:.4rem;text-align:left}}</style><h1>Evidence Metadata Report</h1><table><thead><tr><th>File</th><th>Format</th><th>Bytes</th><th>SHA-256</th><th>Severity</th><th>Anomalies</th></tr></thead><tbody>{rows}</tbody></table>";
        return File.WriteAllTextAsync(outputPath, html, cancellationToken);
    }
    private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value);
}