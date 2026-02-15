using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using VerboVision.PresentationLayer.Middlewares;

namespace VerboVision.Tests.Middlewares;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_NoException_DoesNotModifyResponse()
    {
        // ARRANGE
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        Task next(HttpContext _)
        {
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        }
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // ACT
        await middleware.InvokeAsync(context);

        // ASSERT
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400AndJson()
    {
        // ARRANGE
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        static Task next(HttpContext _) => throw new ArgumentException("Invalid argument");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // ACT
        await middleware.InvokeAsync(context);

        // ASSERT
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Contains("application/json", context.Response.ContentType ?? "");
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("message", out var msg));
        Assert.Equal("Invalid argument", msg.GetString());
        Assert.True(doc.RootElement.TryGetProperty("statusCode", out var code));
        Assert.Equal(400, code.GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        // ARRANGE
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        static Task next(HttpContext _) => throw new InvalidOperationException("Something failed");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // ACT
        await middleware.InvokeAsync(context);

        // ASSERT
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_404Response_Handled()
    {
        // ARRANGE
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Response.StatusCode = 404;
        Task next(HttpContext _)
        {
            context.Response.StatusCode = 404;
            return Task.CompletedTask;
        }
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // ACT
        await middleware.InvokeAsync(context);

        // ASSERT
        Assert.Equal(404, context.Response.StatusCode);
    }
}
