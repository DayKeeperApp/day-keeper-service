using Asp.Versioning;
using DayKeeper.Application.DTOs.Sync;
using DayKeeper.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DayKeeper.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed partial class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [HttpPost("pull")]
    [ProducesResponseType(typeof(SyncPullResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Pull(
        [FromBody] SyncPullRequest request,
        CancellationToken cancellationToken)
    {
        LogSyncPull(_logger, request.Cursor, request.SpaceId);

        var response = await _syncService.PullAsync(
            request.Cursor,
            request.SpaceId,
            request.Limit,
            cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [HttpPost("push")]
    [ProducesResponseType(typeof(SyncPushResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Push(
        [FromBody] SyncPushRequest request,
        CancellationToken cancellationToken)
    {
        LogSyncPush(_logger, request.Changes.Count);

        var response = await _syncService.PushAsync(
            request.Changes,
            cancellationToken).ConfigureAwait(false);

        return Ok(response);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Sync pull requested. Cursor: {Cursor}, SpaceId: {SpaceId}")]
    private static partial void LogSyncPull(ILogger logger, long? cursor, Guid? spaceId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Sync push requested with {ChangeCount} changes")]
    private static partial void LogSyncPush(ILogger logger, int changeCount);
}
