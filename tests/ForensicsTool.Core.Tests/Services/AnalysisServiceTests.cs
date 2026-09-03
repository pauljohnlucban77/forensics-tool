using ForensicsTool.Core.Detection;
using ForensicsTool.Core.FileIdentification;
using ForensicsTool.Core.Hashing;
using ForensicsTool.Core.Interfaces;
using ForensicsTool.Core.Services;

namespace ForensicsTool.Core.Tests.Services;

public class AnalysisServiceTests
{
    [Fact]
    public async Task Reports_empty_unsupported_file_without_changing_it()
    {
        var path = Path.Combine(Path.GetTempPath(), $"forensic-test-{Guid.NewGuid():N}.bin");
        await File.WriteAllBytesAsync(path, []);
        try
        {
            var before = File.GetLastWriteTimeUtc(path);
            var service = new AnalysisService(new MagicByteFileTypeDetector(), Array.Empty<IMetadataExtractor>(), new AnomalyDetector(), new StreamingHasher());
            var report = await service.AnalyzeAsync(path);
            Assert.Equal("UNKNOWN", report.DetectedFormat);
            Assert.Contains(report.Anomalies, anomaly => anomaly.Code == "EMPTY_FILE");
            Assert.Contains(report.Anomalies, anomaly => anomaly.Code == "UNSUPPORTED_FORMAT");
            Assert.Equal(before, File.GetLastWriteTimeUtc(path));
        }
        finally { File.Delete(path); }
    }
}