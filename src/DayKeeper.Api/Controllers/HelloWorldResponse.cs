namespace DayKeeper.Api.Controllers;

public sealed record HelloWorldResponse(
    string Message,
    DateTime Timestamp,
    string Version
);
