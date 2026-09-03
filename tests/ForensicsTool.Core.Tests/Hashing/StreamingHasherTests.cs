using System.Text;
using ForensicsTool.Core.Hashing;

namespace ForensicsTool.Core.Tests.Hashing;

public class StreamingHasherTests
{
    [Fact]
    public async Task Computes_reference_hashes()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("ForensicsTool"));
        var hashes = await new StreamingHasher().ComputeAsync(stream);
        Assert.Equal("fd7ebc228fa96e03792be2210bd90081", hashes.Md5);
        Assert.Equal("e0582c978ab2ab3e1ca0b4ea7d2811b97ce2d8cc", hashes.Sha1);
        Assert.Equal("29bf50ecd8661be7ec0f1f3494453fee6a07d66274d79068cb0e4824cb08db02", hashes.Sha256);
    }
}