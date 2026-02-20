using Microsoft.AspNetCore.Mvc;

namespace DayKeeper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed partial class HelloWorldController : ControllerBase
{
    private readonly ILogger<HelloWorldController> _logger;

    public HelloWorldController(ILogger<HelloWorldController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HelloWorldResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        LogHelloWorldHit(_logger);

        return Ok(new HelloWorldResponse(
            Message: "Hello from DayKeeper!",
            Timestamp: DateTime.UtcNow,
            Version: "1.0.0"
        ));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "HelloWorld endpoint hit")]
    private static partial void LogHelloWorldHit(ILogger logger);
}

public sealed record HelloWorldResponse(
    string Message,
    DateTime Timestamp,
    string Version
);
