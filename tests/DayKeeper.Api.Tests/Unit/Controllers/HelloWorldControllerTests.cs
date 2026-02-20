using DayKeeper.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DayKeeper.Api.Tests.Unit.Controllers;

public class HelloWorldControllerTests
{
    private readonly HelloWorldController _sut;

    public HelloWorldControllerTests()
    {
        var logger = Substitute.For<ILogger<HelloWorldController>>();
        _sut = new HelloWorldController(logger);
    }

    [Fact]
    public void Get_ShouldReturnOkResult()
    {
        // Act
        var result = _sut.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Get_ShouldReturnHelloWorldResponse()
    {
        // Act
        var result = _sut.Get() as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var response = result!.Value as HelloWorldResponse;
        response.Should().NotBeNull();
        response!.Message.Should().Be("Hello from DayKeeper!");
        response.Version.Should().Be("1.0.0");
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
