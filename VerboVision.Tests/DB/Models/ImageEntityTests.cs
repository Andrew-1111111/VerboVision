using VerboVision.DataLayer.DB.Models;
using VerboVision.DataLayer.Dto;

namespace VerboVision.Tests.DB.Models;

public class ImageEntityTests
{
    private const string ValidUrl = "https://example.com/image.jpg";
    private const string ValidFileName = "image.jpg";
    private const string ValidHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void Constructor_ValidArgs_CreatesEntity()
    {
        // ARRANGE
        // (ValidUrl, ValidFileName, ValidHash)

        // ACT
        var entity = new ImageEntity(ValidUrl, ValidFileName, ValidHash);

        // ASSERT
        Assert.Equal(ValidUrl, entity.Url);
        Assert.Equal(ValidFileName, entity.FileName);
        Assert.Equal(ValidHash, entity.FileSha256Hash);
        Assert.Null(entity.AiResponse);
    }

    [Fact]
    public void Constructor_WithAiResponseAndSubjects_SetsValues()
    {
        // ARRANGE
        var subjects = new CoreSubjectsWrapper();
        subjects.Add(new CoreSubjectDto { Name = "стул", Materials = [new MaterialDto { Name = "дерево" }] });

        // ACT
        var entity = new ImageEntity(ValidUrl, ValidFileName, ValidHash, null, "AI said something", subjects);

        // ASSERT
        Assert.Equal("AI said something", entity.AiResponse);
        Assert.Equal(1, entity.CoreSubjects!.Count);
    }

    [Fact]
    public void Constructor_NullUrl_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (null url, valid fileName and hash)

        // ACT & ASSERT
        Assert.Throws<ArgumentNullException>(() =>
            new ImageEntity(null!, ValidFileName, ValidHash));
    }

    [Fact]
    public void Constructor_InvalidHashLength_ThrowsArgumentException()
    {
        // ARRANGE
        var shortHash = "short";

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() =>
            new ImageEntity(ValidUrl, ValidFileName, shortHash));
    }

    [Fact]
    public void Constructor_UrlTooShort_ThrowsArgumentException()
    {
        // ARRANGE
        var shortUrl = "http://a.b";

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() =>
            new ImageEntity(shortUrl, ValidFileName, ValidHash));
    }

    [Fact]
    public void Constants_HaveExpectedValues()
    {
        // ARRANGE
        // (no setup)

        // ACT & ASSERT
        Assert.Equal(11, ImageEntity.MIN_URL_LENGTH);
        Assert.Equal(2048, ImageEntity.MAX_URL_LENGTH);
        Assert.Equal(64, ImageEntity.FILE_HASH_LENGTH);
        Assert.Equal(100_000, ImageEntity.MAX_TEXT_LENGTH);
    }
}
