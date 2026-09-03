# ForensicsTool

ForensicsTool is an offline .NET 8 MVP for cross-platform digital evidence metadata analysis. It opens evidence read-only, streams MD5/SHA-1/SHA-256 reference hashes, identifies common formats by signatures, extracts supported metadata, records anomalies, and writes reports outside the evidence directory.

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/ForensicsTool.Cli -- analyze samples --recursive --output reports --format json --analyst Analyst
```

Formats: JPEG/TIFF/PNG/WEBP (MetadataExtractor), PDF (PdfPig), and DOCX/XLSX/PPTX (OpenXML). Unsupported or malformed files still receive safe file-level results where possible. SHA-256 is the primary integrity hash; MD5 and SHA-1 are legacy/reference values and are not secure integrity algorithms.

See [docs/forensic-methodology.md](docs/forensic-methodology.md) and [docs/architecture.md](docs/architecture.md). This is a metadata-analysis utility, not a complete forensic suite. Metadata can be missing, stripped, forged, generated, inconsistent, or changed during transfer/conversion. Use only with authorization and follow applicable law and evidence-handling policy.