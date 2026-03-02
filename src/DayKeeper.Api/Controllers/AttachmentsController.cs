using Asp.Versioning;
using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DayKeeper.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed partial class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(
        IAttachmentService attachmentService,
        ITenantContext tenantContext,
        ILogger<AttachmentsController> logger)
    {
        _attachmentService = attachmentService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a file attachment and associates it with a parent entity.
    /// Exactly one parent FK (calendarEventId, taskItemId, or personId) must be provided.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(AttachmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] Guid? calendarEventId,
        [FromForm] Guid? taskItemId,
        [FromForm] Guid? personId,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.CurrentTenantId is not { } tenantId)
        {
            return BadRequest(new { error = "X-Tenant-Id header is required." });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required." });
        }

        try
        {
            await using var stream = file.OpenReadStream();

            var attachment = await _attachmentService.CreateAttachmentAsync(
                tenantId,
                calendarEventId,
                taskItemId,
                personId,
                file.FileName,
                file.ContentType,
                stream,
                cancellationToken).ConfigureAwait(false);

            LogAttachmentUploaded(_logger, attachment.Id, attachment.FileName);

            return CreatedAtAction(
                nameof(Download),
                new { id = attachment.Id },
                MapToResponse(attachment));
        }
        catch (InputValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Downloads an attachment file by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var (attachment, stream) = await _attachmentService.GetAttachmentStreamAsync(
                id, cancellationToken).ConfigureAwait(false);

            LogAttachmentDownloaded(_logger, id, attachment.FileName);

            return File(stream, attachment.ContentType, attachment.FileName);
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Retrieves attachment metadata without downloading the file.
    /// </summary>
    [HttpGet("{id:guid}/metadata")]
    [ProducesResponseType(typeof(AttachmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMetadata(
        Guid id,
        CancellationToken cancellationToken)
    {
        var attachment = await _attachmentService.GetAttachmentAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (attachment is null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(attachment));
    }

    /// <summary>
    /// Deletes an attachment (soft-deletes metadata, removes physical file).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _attachmentService.DeleteAttachmentAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (!deleted)
        {
            return NotFound();
        }

        LogAttachmentDeleted(_logger, id);
        return NoContent();
    }

    // ── Response DTO ─────────────────────────────────────────

    private sealed record AttachmentResponse(
        Guid Id,
        Guid TenantId,
        Guid? CalendarEventId,
        Guid? TaskItemId,
        Guid? PersonId,
        string FileName,
        string ContentType,
        long FileSize,
        DateTime CreatedAt);

    private static AttachmentResponse MapToResponse(Domain.Entities.Attachment a) =>
        new(a.Id, a.TenantId, a.CalendarEventId, a.TaskItemId, a.PersonId,
            a.FileName, a.ContentType, a.FileSize, a.CreatedAt);

    // ── Log messages ─────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Attachment uploaded. Id: {AttachmentId}, FileName: {FileName}")]
    private static partial void LogAttachmentUploaded(ILogger logger, Guid attachmentId, string fileName);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Attachment downloaded. Id: {AttachmentId}, FileName: {FileName}")]
    private static partial void LogAttachmentDownloaded(ILogger logger, Guid attachmentId, string fileName);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Attachment deleted. Id: {AttachmentId}")]
    private static partial void LogAttachmentDeleted(ILogger logger, Guid attachmentId);
}
