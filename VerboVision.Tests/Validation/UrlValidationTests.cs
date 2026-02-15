namespace VerboVision.Tests.Validation;

public class UrlValidationTests
{
    [Theory]
    [InlineData("https://example.com/image.jpg")]
    [InlineData("http://site.ru/path/to/photo.JPG")]
    public void IsValidJpgUrl_ValidJpgUrls_ReturnsTrue(string url)
    {
        // ARRANGE
        var input = url;

        // ACT
        var result = VerboVision.PresentationLayer.Validation.UrlValidation.IsValidJpgUrl(ref input);

        // ASSERT
        Assert.True(result);
        Assert.Equal(url.Trim(), input);
    }

    [Theory]
    [InlineData("https://example.com/image.png")]
    [InlineData("https://example.com/image.gif")]
    [InlineData("https://example.com/image")]
    [InlineData("https://example.com/")]
    [InlineData("ftp://example.com/image.jpg")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidJpgUrl_InvalidOrNonJpg_ReturnsFalse(string url)
    {
        // ARRANGE
        var input = url;

        // ACT
        var result = VerboVision.PresentationLayer.Validation.UrlValidation.IsValidJpgUrl(ref input);

        // ASSERT
        Assert.False(result);
    }

    [Fact]
    public void IsValidJpgUrl_Null_ReturnsFalse()
    {
        // ARRANGE
        string? url = null;

        // ACT
        var result = VerboVision.PresentationLayer.Validation.UrlValidation.IsValidJpgUrl(ref url!);

        // ASSERT
        Assert.False(result);
    }

    [Fact]
    public void IsValidJpgUrl_TrimsWhitespace()
    {
        // ARRANGE
        var url = "  https://example.com/img.jpg  ";

        // ACT
        var result = VerboVision.PresentationLayer.Validation.UrlValidation.IsValidJpgUrl(ref url);

        // ASSERT
        Assert.True(result);
        Assert.Equal("https://example.com/img.jpg", url);
    }

    [Fact]
    public void IsValidJpgUrl_InvalidUri_ReturnsFalse()
    {
        // ARRANGE
        var url = "not-a-valid-uri";

        // ACT
        var result = VerboVision.PresentationLayer.Validation.UrlValidation.IsValidJpgUrl(ref url);

        // ASSERT
        Assert.False(result);
    }

    [Fact]
    public void IsValidJpgUrl_ExtensionJpeg_ReturnsFalse()
    {
        // ARRANGE
        var url = "https://example.com/123.JPEG";

        // ACT
        var result = VerboVision.PresentationLayer.Validation.UrlValidation.IsValidJpgUrl(ref url);

        // ASSERT
        Assert.False(result);
    }
}
