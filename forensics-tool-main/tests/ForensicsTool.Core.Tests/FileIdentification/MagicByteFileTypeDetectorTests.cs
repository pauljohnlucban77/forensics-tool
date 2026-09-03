using ForensicsTool.Core.FileIdentification;

namespace ForensicsTool.Core.Tests.FileIdentification;

public class MagicByteFileTypeDetectorTests
{
    [Fact]
    public void Signature_wins_over_misleading_extension()
    {
        var result = new MagicByteFileTypeDetector().Detect("%PDF-1.7"u8, ".jpg");
        Assert.Equal("PDF", result);
    }
}