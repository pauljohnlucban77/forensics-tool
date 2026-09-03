using ForensicsTool.Core.FileIdentification;
using ForensicsTool.Core.Hashing;
using ForensicsTool.Core.Interfaces;
using ForensicsTool.Core.Models;

namespace ForensicsTool.Core.Services;

public sealed class AnalysisService(
    IFileTypeDetector detector,
    IEnumerable<IMetadataExtractor> extractors,
    IAnomalyDetector anomalyDetector,
    StreamingHasher hasher) : IAnalysisService
{
    public async Task<FileForensicReport> AnalyzeAsync(string path, string? analyst = null, CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("Evidence file was not found.", path);
        var extension = info.Extension;
        HashValues hashes;
        try { await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true); hashes = await hasher.ComputeAsync(stream, cancellationToken); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { throw new IOException($"Unable to hash evidence: {path}", ex); }

        var header = new byte[16];
        await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)) _ = await stream.ReadAsync(header, cancellationToken);
        var format = detector.Detect(header, extension);
        var anomalies = new List<ForensicAnomaly>();
        if (info.Length == 0) anomalies.Add(new("EMPTY_FILE", SeverityLevel.Warning, "The evidence file is empty."));
        var extractor = extractors.FirstOrDefault(x => x.CanHandle(format));
        ExtractionResult extraction = ExtractionResult.Empty;
        if (extractor is null) anomalies.Add(new("UNSUPPORTED_FORMAT", SeverityLevel.Info, $"No metadata parser is registered for {format}."));
        else
        {
            try { await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true); extraction = await extractor.ExtractAsync(stream, cancellationToken); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException)
            { anomalies.Add(new("METADATA_PARSE_FAILURE", SeverityLevel.Warning, $"Metadata parsing failed: {ex.Message}")); }
        }
        anomalies.AddRange(extraction.Anomalies);
        var report = new FileForensicReport(info.Name, info.FullName, info.Length, info.LastWriteTimeUtc, info.CreationTimeUtc, format, extension, hashes.Md5, hashes.Sha1, hashes.Sha256, extraction.Properties, extraction.Gps, anomalies, new(DateTimeOffset.UtcNow, analyst, "metadata-analysis", hashes.Sha256, info.Length), extraction.IsPartial);
        return report with { Anomalies = anomalyDetector.Detect(report) };
    }

    public async IAsyncEnumerable<FileForensicReport> AnalyzeManyAsync(IEnumerable<string> paths, string? analyst = null, int parallelism = 2, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var gate = new SemaphoreSlim(Math.Max(1, parallelism));
        var tasks = paths.Select(async path => { await gate.WaitAsync(cancellationToken); try { return await AnalyzeAsync(path, analyst, cancellationToken); } finally { gate.Release(); } }).ToList();
        while (tasks.Count > 0) { var complete = await Task.WhenAny(tasks); tasks.Remove(complete); yield return await complete; }
    }
}