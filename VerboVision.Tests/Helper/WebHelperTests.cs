using VerboVision.DataLayer.Helper;

namespace VerboVision.Tests.Helper;

public class WebHelperTests
{
    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/jpg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/gif", ".gif")]
    [InlineData("application/pdf", ".pdf")]
    [InlineData("application/octet-stream", ".bin")]
    [InlineData(null, ".bin")]
    [InlineData("", ".bin")]
    public void GetExtensionFromContentType_ReturnsExpected(string? contentType, string expected)
    {
        // ARRANGE
        // (contentType, expected from InlineData)

        // ACT
        var result = WebHelper.GetExtensionFromContentType(contentType);

        // ASSERT
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetExtensionFromContentType_WithCharset_ReturnsExtension()
    {
        // ARRANGE
        var contentType = "image/jpeg; charset=utf-8";

        // ACT
        var result = WebHelper.GetExtensionFromContentType(contentType);

        // ASSERT
        Assert.Equal(".jpg", result);
    }

    [Fact]
    public void CleanFileName_Null_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (null fileName)

        // ACT & ASSERT
        Assert.Throws<ArgumentNullException>(() => WebHelper.CleanFileName(null!));
    }

    [Fact]
    public void CleanFileName_WhitespaceOnly_ThrowsArgumentException()
    {
        // ARRANGE
        // (whitespace-only string)

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() => WebHelper.CleanFileName("   "));
    }

    [Fact]
    public void CleanFileName_ValidName_ReturnsSameOrCleaned()
    {
        // ARRANGE
        var fileName = "image.jpg";

        // ACT
        var result = WebHelper.CleanFileName(fileName);

        // ASSERT
        Assert.Equal("image.jpg", result);
    }

    [Fact]
    public void CleanFileName_InvalidChars_ReplacedWithUnderscore()
    {
        // ARRANGE
        var fileName = "file/name?.jpg";

        // ACT
        var result = WebHelper.CleanFileName(fileName);

        // ASSERT
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("?", result);
    }

    [Fact]
    public void CleanFileName_ReservedName_Prefixed()
    {
        // ARRANGE
        var fileName = "CON.jpg";

        // ACT
        var result = WebHelper.CleanFileName(fileName);

        // ASSERT
        Assert.StartsWith("_", result);
    }

    [Fact]
    public void CleanFileName_StartsWithDot_Prefixed()
    {
        // ARRANGE
        var fileName = ".hidden";

        // ACT
        var result = WebHelper.CleanFileName(fileName);

        // ASSERT
        Assert.StartsWith("_", result);
    }
}
