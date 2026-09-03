using ForensicsTool.Core.Interfaces;
using ForensicsTool.Core.Models;

namespace ForensicsTool.Core.Detection;

public sealed class AnomalyDetector : IAnomalyDetector
{
    public IReadOnlyList<ForensicAnomaly> Detect(FileForensicReport report)
    {
        var anomalies = report.Anomalies.ToList();
        if (report.Gps is { } gps && (gps.Latitude is < -90 or > 90 || gps.Longitude is < -180 or > 180))
            anomalies.Add(new("INVALID_GPS", SeverityLevel.Warning, "GPS coordinates fall outside valid latitude/longitude ranges."));
        if (report.LastWriteTimeUtc > DateTimeOffset.UtcNow.AddMinutes(5) || report.CreationTimeUtc > DateTimeOffset.UtcNow.AddMinutes(5))
            anomalies.Add(new("FUTURE_TIMESTAMP", SeverityLevel.Warning, "A filesystem timestamp is in the future relative to analysis time."));
        if (report.DeclaredExtension is not null && report.DetectedFormat != "UNKNOWN" && !Matches(report.DeclaredExtension, report.DetectedFormat))
            anomalies.Add(new("EXTENSION_MISMATCH", SeverityLevel.Warning, $"Declared extension {report.DeclaredExtension} does not match detected format {report.DetectedFormat}."));
        var embedded = report.Metadata.FirstOrDefault(x => x.Name.Contains("DateTime", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("Created", StringComparison.OrdinalIgnoreCase));
        if (embedded is not null && DateTimeOffset.TryParse(embedded.Value, out var timestamp) && Math.Abs((timestamp - report.LastWriteTimeUtc).TotalMinutes) > 5)
            anomalies.Add(new("FILESYSTEM_EMBEDDED_TIMESTAMP_MISMATCH", SeverityLevel.Warning, "Embedded and filesystem timestamps differ materially; this is an indicator requiring investigation."));
        return anomalies.DistinctBy(a => (a.Code, a.Description)).ToArray();
    }
    private static bool Matches(string extension, string format) => format switch { "JPEG" => extension is ".jpg" or ".jpeg", "TIFF" => extension is ".tif" or ".tiff", _ => extension.Equals("." + format, StringComparison.OrdinalIgnoreCase) };
}