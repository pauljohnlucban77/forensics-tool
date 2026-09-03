using ForensicsTool.Core.Models;

namespace ForensicsTool.Core.Interfaces;

public interface IFileTypeDetector { string Detect(ReadOnlySpan<byte> header, string extension); }
public interface IMetadataExtractor { bool CanHandle(string format); Task<ExtractionResult> ExtractAsync(Stream content, CancellationToken cancellationToken); }
public interface IAnomalyDetector { IReadOnlyList<ForensicAnomaly> Detect(FileForensicReport report); }
public interface IAnalysisService
{
    Task<FileForensicReport> AnalyzeAsync(string path, string? analyst = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<FileForensicReport> AnalyzeManyAsync(IEnumerable<string> paths, string? analyst = null, int parallelism = 2, CancellationToken cancellationToken = default);
}
public interface IReportExporter { Task ExportAsync(IReadOnlyCollection<FileForensicReport> reports, string outputPath, CancellationToken cancellationToken = default); }