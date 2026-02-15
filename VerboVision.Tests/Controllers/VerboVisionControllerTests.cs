using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VerboVision.DataLayer.Dto;
using VerboVision.PresentationLayer.Controllers.Api;
using VerboVision.PresentationLayer.Dto.Api;

namespace VerboVision.Tests.Controllers;

public class VerboVisionControllerTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<DataLayer.Repositories.Interfaces.IImageRepository> _repoMock;
    private readonly Mock<ILogger<VerboVisionController>> _loggerMock;

    public VerboVisionControllerTests()
    {
        _configMock = new Mock<IConfiguration>();
        _repoMock = new Mock<DataLayer.Repositories.Interfaces.IImageRepository>();
        _loggerMock = new Mock<ILogger<VerboVisionController>>();
    }

    private VerboVisionController CreateController(string? authKey = "test-key")
    {
        _configMock.Setup(c => c["GigaChat:AuthorizationKey"]).Returns(authKey);
        return new VerboVisionController(_configMock.Object, _repoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetImageInfo_InvalidUrl_ReturnsBadRequest()
    {
        // ARRANGE
        var controller = CreateController();
        var imageUrl = "https://example.com/image.png";

        // ACT
        var result = await controller.GetImageInfo(imageUrl);

        // ASSERT
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetImageInfo_NoAuthKey_Returns500()
    {
        // ARRANGE
        var controller = CreateController(authKey: null);
        var imageUrl = "https://example.com/photo.jpg";

        // ACT
        var result = await controller.GetImageInfo(imageUrl);

        // ASSERT
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetImageInfo_ValidUrl_ReturnsOkWithDto()
    {
        // ARRANGE
        var guid = Guid.NewGuid();
        var subjects = new CoreSubjectsWrapper();
        subjects.Add(new CoreSubjectDto { Name = "стул", Materials = [new MaterialDto { Name = "дерево" }] });
        _repoMock
            .Setup(r => r.AnalyzeImageAsync(It.IsAny<string>(), "https://example.com/photo.jpg", default))
            .Returns(Task.FromResult<(Guid?, CoreSubjectsWrapper)>((guid, subjects)));
        var controller = CreateController();

        // ACT
        var result = await controller.GetImageInfo("https://example.com/photo.jpg");

        // ASSERT
        var okResult = Assert.IsType<ActionResult<UploadImgDto>>(result);
        Assert.NotNull(okResult.Result);
        var ok = Assert.IsType<OkObjectResult>(okResult.Result);
        var dto = Assert.IsType<UploadImgDto>(ok.Value);
        Assert.Equal(guid, dto.Id);
        Assert.Equal(1, dto.CoreSubjects.Count);
    }

    [Fact]
    public async Task GetImageInfo_RepositoryReturnsNullUuid_ReturnsBadRequest()
    {
        // ARRANGE
        _repoMock
            .Setup(r => r.AnalyzeImageAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.FromResult<(Guid?, CoreSubjectsWrapper)>((null, new CoreSubjectsWrapper())));
        var controller = CreateController();

        // ACT
        var result = await controller.GetImageInfo("https://example.com/photo.jpg");

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetImageInfo_UnauthorizedAccessException_Returns401()
    {
        // ARRANGE
        _repoMock
            .Setup(r => r.AnalyzeImageAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController();

        // ACT
        var result = await controller.GetImageInfo("https://example.com/photo.jpg");

        // ASSERT
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task CheckImageInfo_EmptyRequestId_ReturnsBadRequest()
    {
        // ARRANGE
        var controller = CreateController();
        var requestId = Guid.Empty;
        var subjects = new List<string> { "ручка" };

        // ACT
        var result = await controller.CheckImageInfo(requestId, subjects);

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CheckImageInfo_EmptySubjects_ReturnsBadRequest()
    {
        // ARRANGE
        var controller = CreateController();
        var requestId = Guid.NewGuid();
        var subjects = new List<string>();

        // ACT
        var result = await controller.CheckImageInfo(requestId, subjects);

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CheckImageInfo_NullSubjects_ReturnsBadRequest()
    {
        // ARRANGE
        var controller = CreateController();
        var requestId = Guid.NewGuid();

        // ACT
        var result = await controller.CheckImageInfo(requestId, null!);

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CheckImageInfo_NoAuthKey_Returns500()
    {
        // ARRANGE
        var controller = CreateController(authKey: null);
        var requestId = Guid.NewGuid();
        var subjects = new List<string> { "ручка" };

        // ACT
        var result = await controller.CheckImageInfo(requestId, subjects);

        // ASSERT
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CheckImageInfo_Success_ReturnsOkWithList()
    {
        // ARRANGE
        var requestId = Guid.NewGuid();
        var wrapper = new CoreSubjectsWrapper();
        wrapper.Add(new CoreSubjectDto { Name = "ручка", Materials = [new MaterialDto { Name = "пластик" }] });
        _repoMock
            .Setup(r => r.AnalyzeSubjectsAsync(It.IsAny<string>(), requestId, It.IsAny<List<string>>(), default))
            .ReturnsAsync(wrapper);
        var controller = CreateController();
        var subjects = new List<string> { "ручка" };

        // ACT
        var result = await controller.CheckImageInfo(requestId, subjects);

        // ASSERT
        var okResult = Assert.IsType<ActionResult<List<CoreSubjectDto>>>(result);
        var ok = Assert.IsType<OkObjectResult>(okResult.Result);
        var returned = ok.Value;
        var wrapperReturned = Assert.IsType<CoreSubjectsWrapper>(returned);
        Assert.Equal(1, wrapperReturned.Count);
        Assert.Equal("ручка", wrapperReturned[0].Name);
    }

    [Fact]
    public async Task CheckImageInfo_RepositoryReturnsEmpty_ReturnsBadRequest()
    {
        // ARRANGE
        _repoMock
            .Setup(r => r.AnalyzeSubjectsAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<List<string>>(), default))
            .ReturnsAsync(new CoreSubjectsWrapper());
        var controller = CreateController();
        var requestId = Guid.NewGuid();
        var subjects = new List<string> { "ручка" };

        // ACT
        var result = await controller.CheckImageInfo(requestId, subjects);

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
