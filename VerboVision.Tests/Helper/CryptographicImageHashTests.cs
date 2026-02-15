using System.Security.Cryptography;
using VerboVision.DataLayer.Helper;

namespace VerboVision.Tests.Helper;

public class CryptographicImageHashTests
{
    [Fact]
    public void ComputeSha256Hash_Bytes_Returns64CharHexLower()
    {
        // ARRANGE
        var bytes = new byte[] { 1, 2, 3, 4, 5 };

        // ACT
        var hash = CryptographicImageHash.ComputeSha256Hash(bytes);

        // ASSERT
        Assert.Equal(64, hash.Length);
        Assert.True(hash.All(c => char.IsAsciiHexDigit(c) && (c is >= '0' and <= '9' or >= 'a' and <= 'f')));
    }

    [Fact]
    public void ComputeSha256Hash_NullBytes_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (null bytes)

        // ACT & ASSERT
        Assert.Throws<ArgumentNullException>(() => CryptographicImageHash.ComputeSha256Hash((byte[])null!));
    }

    [Fact]
    public void ComputeSha256Hash_Stream_SameAsBytes()
    {
        // ARRANGE
        var bytes = new byte[] { 10, 20, 30 };
        using var stream = new MemoryStream(bytes);

        // ACT
        var hashStream = CryptographicImageHash.ComputeSha256Hash(stream);
        var hashBytes = CryptographicImageHash.ComputeSha256Hash(bytes);

        // ASSERT
        Assert.Equal(hashBytes, hashStream);
    }

    [Fact]
    public void ComputeSha256Hash_NullStream_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (null stream)

        // ACT & ASSERT
        Assert.Throws<ArgumentNullException>(() => CryptographicImageHash.ComputeSha256Hash((Stream)null!));
    }

    [Fact]
    public void ComputeSha256Hash_EmptyPath_ThrowsArgumentException()
    {
        // ARRANGE
        // (empty path)

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() => CryptographicImageHash.ComputeSha256Hash(""));
    }

    [Fact]
    public void ComputeSha256Hash_FileNotFound_ThrowsFileNotFoundException()
    {
        // ARRANGE
        var path = "C:\\nonexistent_file_12345.jpg";

        // ACT & ASSERT
        Assert.Throws<FileNotFoundException>(() =>
            CryptographicImageHash.ComputeSha256Hash(path));
    }

    [Fact]
    public void ComputeSha256Hash_Deterministic()
    {
        // ARRANGE
        var bytes = new byte[] { 1, 2, 3 };

        // ACT
        var h1 = CryptographicImageHash.ComputeSha256Hash(bytes);
        var h2 = CryptographicImageHash.ComputeSha256Hash(bytes);

        // ASSERT
        Assert.Equal(h1, h2);
    }
}
