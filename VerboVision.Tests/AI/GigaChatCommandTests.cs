using VerboVision.DataLayer.AI;

namespace VerboVision.Tests.AI;

public class GigaChatCommandTests
{
    [Fact]
    public void GetSubjectsPrompt_ReturnsNonEmptyString()
    {
        // ARRANGE
        // (no parameters)

        // ACT
        var prompt = GigaChatCommand.GetSubjectsPrompt();

        // ASSERT
        Assert.False(string.IsNullOrWhiteSpace(prompt));
        Assert.Contains("объект", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("материал", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetMaterialsPrompt_IncludesSubjects()
    {
        // ARRANGE
        var subjects = new List<string> { "ежедневник", "ручка" };

        // ACT
        var prompt = GigaChatCommand.GetMaterialsPrompt(subjects);

        // ASSERT
        Assert.Contains("ежедневник", prompt);
        Assert.Contains("ручка", prompt);
        Assert.Contains("материал", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetMaterialsPrompt_FormatExample()
    {
        // ARRANGE
        var subjects = new List<string> { "стол" };

        // ACT
        var prompt = GigaChatCommand.GetMaterialsPrompt(subjects);

        // ASSERT
        Assert.Contains("стол", prompt);
        Assert.Contains(":", prompt);
    }

    [Fact]
    public async Task CheckTextAsync_NullKey_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (empty auth key, valid subjects)

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            GigaChatCommand.CheckTextAsync("", new List<string> { "x" }));
    }

    [Fact]
    public async Task CheckTextAsync_EmptySubjects_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (valid key, empty subjects list)

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            GigaChatCommand.CheckTextAsync("fake-key", new List<string>()));
    }

    [Fact]
    public async Task CheckImageAsync_NullKey_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (empty key, valid bytes and fileName)

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            GigaChatCommand.CheckImageAsync("", new byte[] { 1, 2, 3 }, "test.jpg"));
    }

    [Fact]
    public async Task CheckImageAsync_EmptyBytes_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (valid key, empty bytes, valid fileName)

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            GigaChatCommand.CheckImageAsync("key", Array.Empty<byte>(), "test.jpg"));
    }

    [Fact]
    public async Task CheckImageAsync_EmptyFileName_ThrowsArgumentNullException()
    {
        // ARRANGE
        // (valid key and bytes, empty fileName)

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            GigaChatCommand.CheckImageAsync("key", new byte[] { 1 }, ""));
    }
}
