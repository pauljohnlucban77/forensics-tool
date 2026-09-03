using System.Security.Cryptography;
using ForensicsTool.Core.Models;

namespace ForensicsTool.Core.Hashing;

public sealed class StreamingHasher
{
    public async Task<HashValues> ComputeAsync(Stream input, CancellationToken cancellationToken = default)
    {
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[1024 * 128];
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            md5.AppendData(buffer, 0, read); sha1.AppendData(buffer, 0, read); sha256.AppendData(buffer, 0, read);
        }
        return new(ToHex(md5.GetHashAndReset()), ToHex(sha1.GetHashAndReset()), ToHex(sha256.GetHashAndReset()));
    }

    private static string ToHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}