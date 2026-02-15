using VerboVision.DataLayer.Dto;

namespace VerboVision.Tests.Dto;

public class CoreSubjectsWrapperTests
{
    [Fact]
    public void ParseMultiple_EmptyString_ReturnsEmptyWrapper()
    {
        // ARRANGE
        // (empty string)

        // ACT
        var result = CoreSubjectsWrapper.ParseMultiple("");

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void ParseMultiple_NullString_ReturnsEmptyWrapper()
    {
        // ARRANGE
        // (null string)

        // ACT
        var result = CoreSubjectsWrapper.ParseMultiple((string)null!);

        // ASSERT
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void ParseMultiple_NewlineSeparated_ParsesAll()
    {
        // ARRANGE
        var input = "стул: дерево\nручка: пластик\nстол: стекло";

        // ACT
        var result = CoreSubjectsWrapper.ParseMultiple(input);

        // ASSERT
        Assert.Equal(3, result.Count);
        Assert.Equal("стул", result[0].Name);
        Assert.Equal("ручка", result[1].Name);
        Assert.Equal("стол", result[2].Name);
    }

    [Fact]
    public void ParseMultiple_SemicolonSeparated_ParsesAll()
    {
        // ARRANGE
        var input = "a: x; b: y";

        // ACT
        var result = CoreSubjectsWrapper.ParseMultiple(input);

        // ASSERT
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ParseMultiple_ListOfStrings_ParsesValidLines()
    {
        // ARRANGE
        var lines = new List<string> { "ежедневник: бумага, картон", "ручка: пластик" };

        // ACT
        var result = CoreSubjectsWrapper.ParseMultiple(lines);

        // ASSERT
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ParseMultiple_ListNull_ReturnsEmpty()
    {
        // ARRANGE
        // (null list)

        // ACT
        var result = CoreSubjectsWrapper.ParseMultiple((List<string>)null!);

        // ASSERT
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void Add_AddsItem()
    {
        // ARRANGE
        var wrapper = new CoreSubjectsWrapper();
        var item = new CoreSubjectDto { Name = "test", Materials = [new MaterialDto { Name = "m" }] };

        // ACT
        wrapper.Add(item);

        // ASSERT
        Assert.Equal(1, wrapper.Count);
    }

    [Fact]
    public void Add_Null_DoesNotThrow()
    {
        // ARRANGE
        var wrapper = new CoreSubjectsWrapper();

        // ACT
        wrapper.Add(null!);

        // ASSERT
        Assert.Equal(0, wrapper.Count);
    }

    [Fact]
    public void Remove_ExistingItem_ReturnsTrue()
    {
        // ARRANGE
        var item = new CoreSubjectDto { Name = "x" };
        var wrapper = new CoreSubjectsWrapper();
        wrapper.Add(item);

        // ACT
        var removed = wrapper.Remove(item);

        // ASSERT
        Assert.True(removed);
        Assert.Equal(0, wrapper.Count);
    }

    [Fact]
    public void Clear_EmptiesList()
    {
        // ARRANGE
        var wrapper = new CoreSubjectsWrapper();
        wrapper.Add(new CoreSubjectDto { Name = "a" });

        // ACT
        wrapper.Clear();

        // ASSERT
        Assert.Equal(0, wrapper.Count);
    }

    [Fact]
    public void Equals_SameContent_SameCountAndStructure()
    {
        // ARRANGE
        var a = CoreSubjectsWrapper.ParseMultiple("стул: дерево\nручка: пластик");
        var b = CoreSubjectsWrapper.ParseMultiple("стул: дерево\nручка: пластик");

        // ACT & ASSERT
        Assert.Equal(a.Count, b.Count);
        Assert.Equal(a[0].Name, b[0].Name);
        Assert.Equal(a[1].Name, b[1].Name);
    }

    [Fact]
    public void Equals_DifferentCount_ReturnsFalse()
    {
        // ARRANGE
        var a = CoreSubjectsWrapper.ParseMultiple("стул: дерево");
        var b = CoreSubjectsWrapper.ParseMultiple("стул: дерево\nручка: пластик");

        // ACT & ASSERT
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void ToString_ReturnsDescriptive()
    {
        // ARRANGE
        var w = new CoreSubjectsWrapper();
        w.Add(new CoreSubjectDto { Name = "x" });

        // ACT
        var text = w.ToString();

        // ASSERT
        Assert.Contains("1", text);
    }
}
