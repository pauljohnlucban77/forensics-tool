using System.Collections.ObjectModel;

namespace ForensicsTool.Core.Models;

public enum SeverityLevel { Info, Warning, Critical }

public sealed record MetadataProperty(string Name, string Value, string Source, string? Group = null);

public sealed record ForensicAnomaly(string Code, SeverityLevel Severity, string Description);

public sealed record GpsData(double Latitude, double Longitude, double? Altitude = null);

public sealed record ExtractionResult(
    IReadOnlyList<MetadataProperty> Properties,
    GpsData? Gps,
    IReadOnlyList<ForensicAnomaly> Anomalies,
    bool IsPartial = false)
{
    public static ExtractionResult Empty => new([], null, []);
}

public sealed record EvidenceCustodyRecord(
    DateTimeOffset AnalyzedAtUtc,
    string? Analyst,
    string Operation,
    string Sha256,
    long Size);

public sealed record FileForensicReport(
    string FileName,
    string FullPath,
    long Size,
    DateTimeOffset LastWriteTimeUtc,
    DateTimeOffset CreationTimeUtc,
    string DetectedFormat,
    string? DeclaredExtension,
    string Md5,
    string Sha1,
    string Sha256,
    IReadOnlyList<MetadataProperty> Metadata,
    GpsData? Gps,
    IReadOnlyList<ForensicAnomaly> Anomalies,
    EvidenceCustodyRecord Custody,
    bool IsPartial = false)
{
    public SeverityLevel HighestSeverity => Anomalies.Any(a => a.Severity == SeverityLevel.Critical)
        ? SeverityLevel.Critical : Anomalies.Any(a => a.Severity == SeverityLevel.Warning) ? SeverityLevel.Warning : SeverityLevel.Info;
}

public sealed record HashValues(string Md5, string Sha1, string Sha256);