using DocumentFormat.OpenXml.Packaging;
using ForensicsTool.Core.Interfaces;
using ForensicsTool.Core.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using UglyToad.PdfPig;

namespace ForensicsTool.Core.Extractors;

public sealed class ImageMetadataExtractor : IMetadataExtractor
{
    public bool CanHandle(string format) => format is "JPEG" or "TIFF" or "PNG" or "WEBP";
    public Task<ExtractionResult> ExtractAsync(Stream content, CancellationToken cancellationToken)
    {
        var properties = new List<MetadataProperty>();
        GpsData? gps = null;
        try
        {
            foreach (var directory in ImageMetadataReader.ReadMetadata(content))
            foreach (var tag in directory.Tags)
            {
                properties.Add(new(tag.Name, tag.Description ?? string.Empty, "MetadataExtractor", directory.Name));
                if (directory is GpsDirectory gpsDirectory && tag.Name == "GPS Latitude")
                {
                    var coordinates = gpsDirectory.GetGeoLocation();
                    if (coordinates is not null && TryParseGps(coordinates.ToString(), out var latitude, out var longitude)) gps = new(latitude, longitude);
                }
            }
            return Task.FromResult(new ExtractionResult(properties, gps, []));
        }
        catch (Exception ex) when (ex is ImageProcessingException or IOException or InvalidDataException)
        { return Task.FromResult(new ExtractionResult(properties, gps, [new("METADATA_PARSE_FAILURE", SeverityLevel.Warning, ex.Message)], true)); }
    }

    private static bool TryParseGps(string? value, out double latitude, out double longitude)
    {
        latitude = longitude = 0;
        var parts = value?.Split(',', StringSplitOptions.TrimEntries);
        return parts is { Length: 2 } && double.TryParse(parts[0], out latitude) && double.TryParse(parts[1], out longitude);
    }
}

public sealed class PdfMetadataExtractor : IMetadataExtractor
{
    public bool CanHandle(string format) => format == "PDF";
    public Task<ExtractionResult> ExtractAsync(Stream content, CancellationToken cancellationToken)
    {
        var properties = new List<MetadataProperty>();
        try
        {
            using var document = PdfDocument.Open(content);
            var information = document.Information;
            properties.AddRange(new[] { ("Title", information.Title), ("Author", information.Author), ("Subject", information.Subject), ("Keywords", information.Keywords), ("Creator", information.Creator), ("Producer", information.Producer) }
                .Where(p => !string.IsNullOrWhiteSpace(p.Item2)).Select(p => new MetadataProperty(p.Item1, p.Item2!, "PdfPig", "Document Information")));
            return Task.FromResult(new ExtractionResult(properties, null, []));
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or InvalidDataException)
        { return Task.FromResult(new ExtractionResult(properties, null, [new("METADATA_PARSE_FAILURE", SeverityLevel.Warning, ex.Message)], true)); }
    }
}

public sealed class OpenXmlMetadataExtractor : IMetadataExtractor
{
    public bool CanHandle(string format) => format is "DOCX" or "XLSX" or "PPTX";
    public Task<ExtractionResult> ExtractAsync(Stream content, CancellationToken cancellationToken)
    {
        var properties = new List<MetadataProperty>();
        try
        {
            using var package = formatPackage(content);
            var p = package.PackageProperties;
            foreach (var item in new[] { ("Title", p.Title), ("Subject", p.Subject), ("Creator", p.Creator), ("Keywords", p.Keywords), ("Description", p.Description), ("LastModifiedBy", p.LastModifiedBy) })
                if (!string.IsNullOrWhiteSpace(item.Item2)) properties.Add(new(item.Item1, item.Item2!, "OpenXML", "Package Properties"));
            return Task.FromResult(new ExtractionResult(properties, null, []));
        }
        catch (Exception ex) when (ex is OpenXmlPackageException or IOException or InvalidDataException)
        { return Task.FromResult(new ExtractionResult(properties, null, [new("METADATA_PARSE_FAILURE", SeverityLevel.Warning, ex.Message)], true)); }
    }

    private static OpenXmlPackage formatPackage(Stream stream)
    {
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
        if (archive.GetEntry("word/document.xml") is not null) return WordprocessingDocument.Open(stream, false);
        if (archive.GetEntry("ppt/presentation.xml") is not null) return PresentationDocument.Open(stream, false);
        return SpreadsheetDocument.Open(stream, false);
    }
}