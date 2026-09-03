using ForensicsTool.Core.Detection;
using ForensicsTool.Core.Extractors;
using ForensicsTool.Core.FileIdentification;
using ForensicsTool.Core.Hashing;
using ForensicsTool.Core.Interfaces;
using ForensicsTool.Core.Models;
using ForensicsTool.Core.Reporting;
using ForensicsTool.Core.Services;
using Spectre.Console;

if (args.Length == 0 || args[0] is "--help" or "-h") { PrintHelp(); return 2; }
if (args[0] == "help") { PrintHelp(); return 0; }
if (args[0] == "version") { AnsiConsole.MarkupLine("ForensicsTool [grey]1.0.0[/]"); return 0; }
if (args[0] != "analyze") { AnsiConsole.MarkupLine("[red]Unknown command.[/] Use [yellow]help[/]."); return 2; }

var options = Parse(args[1..]);
if (options.Input is null) { AnsiConsole.MarkupLine("[red]An input file or directory is required.[/]"); return 2; }
var paths = ResolvePaths(options.Input, options.Recursive).ToArray();
if (paths.Length == 0) { AnsiConsole.MarkupLine("[red]No evidence files found.[/]"); return 3; }
Directory.CreateDirectory(options.OutputDirectory);

IAnalysisService service = new AnalysisService(new MagicByteFileTypeDetector(), new IMetadataExtractor[] { new ImageMetadataExtractor(), new PdfMetadataExtractor(), new OpenXmlMetadataExtractor() }, new AnomalyDetector(), new StreamingHasher());
var reports = new List<FileForensicReport>();
await foreach (var report in service.AnalyzeManyAsync(paths, options.Analyst, options.Parallelism))
{
	reports.Add(report);
	if (options.Verbose) Console.Error.WriteLine($"{{\"event\":\"analyzed\",\"file\":{System.Text.Json.JsonSerializer.Serialize(report.FileName)},\"sha256\":\"{report.Sha256}\",\"severity\":\"{report.HighestSeverity}\"}}");
}
var duplicateHashes = reports.GroupBy(r => r.Sha256).Where(g => g.Count() > 1).SelectMany(g => g).ToHashSet();
reports = reports.Select(r => duplicateHashes.Contains(r) ? r with { Anomalies = r.Anomalies.Append(new ForensicAnomaly("DUPLICATE_HASH", SeverityLevel.Warning, "Another analyzed evidence file has the same SHA-256 value.")).ToArray() } : r).ToList();
	reports = reports.Select(r => duplicateHashes.Contains(r) ? r with { Anomalies = r.Anomalies.Append(new ForensicAnomaly("DUPLICATE_HASH", SeverityLevel.Info, "Another analyzed evidence file has the same SHA-256 value.")).ToArray() } : r).ToList();
var output = Path.Combine(options.OutputDirectory, $"report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{options.Format}");
IReportExporter exporter = options.Format switch { "csv" => new CsvReportExporter(), "html" => new HtmlReportExporter(), _ => new JsonReportExporter() };
await exporter.ExportAsync(reports, output);
AnsiConsole.MarkupLine($"Analyzed [green]{reports.Count}[/] file(s); report written to [cyan]{Markup.Escape(output)}[/].");
return reports.Any(r => r.HighestSeverity == SeverityLevel.Critical) ? 5 : reports.Any(r => r.HighestSeverity == SeverityLevel.Warning) ? 4 : 0;

static IEnumerable<string> ResolvePaths(string input, bool recursive)
{
	if (File.Exists(input)) return [Path.GetFullPath(input)];
	if (!Directory.Exists(input)) return [];
	return Directory.EnumerateFiles(input, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
}

static void PrintHelp() => Console.WriteLine("ForensicsTool analyze <file-or-directory> [--output DIR] [--format json|csv|html] [--recursive] [--parallelism N] [--analyst NAME] [--verbose]\nForensicsTool version");

static Options Parse(string[] args)
{
	var value = new Options();
	for (var i = 0; i < args.Length; i++)
	{
		switch (args[i])
		{
			case "--output" when i + 1 < args.Length: value.OutputDirectory = args[++i]; break;
			case "--format" when i + 1 < args.Length: value.Format = args[++i].ToLowerInvariant(); break;
			case "--parallelism" when i + 1 < args.Length && int.TryParse(args[++i], out var parallelism): value.Parallelism = Math.Max(1, parallelism); break;
			case "--analyst" when i + 1 < args.Length: value.Analyst = args[++i]; break;
			case "--recursive": value.Recursive = true; break;
			case "--verbose": value.Verbose = true; break;
			default: value.Input ??= args[i]; break;
		}
	}
	if (value.Format is not ("json" or "csv" or "html")) value.Format = "json";
	return value;
}

sealed class Options
{
	public string? Input { get; set; }
	public string OutputDirectory { get; set; } = Path.Combine(Environment.CurrentDirectory, "reports");
	public string Format { get; set; } = "json";
	public bool Recursive { get; set; }
	public int Parallelism { get; set; } = 2;
	public string? Analyst { get; set; }
	public bool Verbose { get; set; }
}
