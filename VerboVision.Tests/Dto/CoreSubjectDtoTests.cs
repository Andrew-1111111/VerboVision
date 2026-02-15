using VerboVision.DataLayer.Dto;

namespace VerboVision.Tests.Dto;

public class CoreSubjectDtoTests
{
    [Theory]
    [InlineData("стул: дерево, металл", "стул", "дерево", "металл")]
    [InlineData("Ручка: пластик, металл, чернила", "Ручка", "пластик", "металл", "чернила")]
    [InlineData("Ежедневник: бумага", "Ежедневник", "бумага")]
    public void Parse_ValidFormat_ReturnsCorrectDto(string input, string expectedName, params string[] expectedMaterials)
    {
        // ARRANGE
        // (input, expectedName, expectedMaterials from InlineData)

        // ACT
        var result = CoreSubjectDto.Parse(input);

        // ASSERT
        Assert.Equal(expectedName, result.Name);
        Assert.Equal(expectedMaterials.Length, result.Materials.Count);
        for (var i = 0; i < expectedMaterials.Length; i++)
            Assert.Equal(expectedMaterials[i], result.Materials[i].Name);
    }

    [Fact]
    public void Parse_EmptyString_ThrowsArgumentException()
    {
        // ARRANGE
        // (empty / whitespace strings)

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() => CoreSubjectDto.Parse(""));
        Assert.Throws<ArgumentException>(() => CoreSubjectDto.Parse("   "));
    }

    [Theory]
    [InlineData("no-colon")]
    [InlineData("onlyone:")]
    public void Parse_NoColonOrInvalidFormat_ThrowsArgumentException(string input)
    {
        // ARRANGE
        // (input from InlineData)

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() => CoreSubjectDto.Parse(input));
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrueAndResult()
    {
        // ARRANGE
        // (valid input string)

        // ACT
        var ok = CoreSubjectDto.TryParse("стол: дерево", out var result);

        // ASSERT
        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal("стол", result!.Name);
        Assert.Single(result.Materials);
        Assert.Equal("дерево", result.Materials[0].Name);
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalseAndNull()
    {
        // ARRANGE
        // (invalid input strings)

        // ACT & ASSERT
        Assert.False(CoreSubjectDto.TryParse("", out var r1));
        Assert.Null(r1);
        Assert.False(CoreSubjectDto.TryParse("bad", out var r2));
        Assert.Null(r2);
    }

    [Fact]
    public void GetMaterialsSummary_ReturnsJoinedNames()
    {
        // ARRANGE
        var dto = new CoreSubjectDto { Name = "x" };
        dto.AddMaterial(new MaterialDto { Name = "a" });
        dto.AddMaterial(new MaterialDto { Name = "b" });

        // ACT
        var summary = dto.GetMaterialsSummary();

        // ASSERT
        Assert.Equal("a, b", summary);
    }

    [Fact]
    public void ContainsMaterial_ExistingMaterial_ReturnsTrue()
    {
        // ARRANGE
        var dto = CoreSubjectDto.Parse("ручка: пластик, металл");

        // ACT & ASSERT
        Assert.True(dto.ContainsMaterial("пластик"));
        Assert.True(dto.ContainsMaterial("ПЛАСТИК"));
    }

    [Fact]
    public void ContainsMaterial_NotExisting_ReturnsFalse()
    {
        // ARRANGE
        var dto = CoreSubjectDto.Parse("ручка: пластик");

        // ACT & ASSERT
        Assert.False(dto.ContainsMaterial("дерево"));
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        // ARRANGE
        var dto = CoreSubjectDto.Parse("стул: дерево, металл");

        // ACT
        var text = dto.ToString();

        // ASSERT
        Assert.Equal("стул: дерево, металл", text);
    }
}
