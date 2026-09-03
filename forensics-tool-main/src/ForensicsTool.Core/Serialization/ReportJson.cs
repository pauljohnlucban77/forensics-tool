using System.Text.Json.Serialization;
using ForensicsTool.Core.Models;

namespace ForensicsTool.Core.Serialization;

[JsonSerializable(typeof(FileForensicReport))]
[JsonSerializable(typeof(IReadOnlyCollection<FileForensicReport>))]
[JsonSerializable(typeof(List<FileForensicReport>))]
[JsonSourceGenerationOptions(WriteIndented = true, Converters = [typeof(JsonStringEnumConverter<SeverityLevel>)])]
public partial class ReportJsonContext : JsonSerializerContext;