using System.Buffers;
using ForensicsTool.Core.Interfaces;

namespace ForensicsTool.Core.FileIdentification;

public sealed class MagicByteFileTypeDetector : IFileTypeDetector
{
    public string Detect(ReadOnlySpan<byte> header, string extension)
    {
        if (header.Length >= 3 && header[..3].SequenceEqual("\xFF\xD8\xFF"u8)) return "JPEG";
        if (header.Length >= 4 && header[..4].SequenceEqual("II*\0"u8)) return "TIFF";
        if (header.Length >= 4 && header[..4].SequenceEqual("MM\0*"u8)) return "TIFF";
        if (header.Length >= 8 && header[..8].SequenceEqual("\x89PNG\r\n\x1A\n"u8)) return "PNG";
        if (header.Length >= 4 && header[..4].SequenceEqual("%PDF"u8)) return "PDF";
        if (header.Length >= 4 && header[..4].SequenceEqual("PK\x03\x04"u8))
            return extension.ToUpperInvariant() switch { ".DOCX" => "DOCX", ".XLSX" => "XLSX", ".PPTX" => "PPTX", _ => "ZIP" };
        return extension.TrimStart('.').ToUpperInvariant() switch { "JPG" => "JPEG", "JPEG" => "JPEG", "WEBP" => "WEBP", _ => "UNKNOWN" };
    }
}