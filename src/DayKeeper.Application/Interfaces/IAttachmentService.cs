using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing attachment metadata and file storage.
/// Orchestrates validation, file persistence, and database operations
/// for the <see cref="Attachment"/> entity.
/// </summary>
public interface IAttachmentService
{
    /// <summary>
    /// Creates a new attachment: validates inputs, saves the file to storage,
    /// and persists the attachment metadata.
    /// </summary>
    /// <param name="tenantId">The tenant that owns this attachment.</param>
    /// <param name="calendarEventId">Parent calendar event (mutually exclusive with other parent FKs).</param>
    /// <param name="taskItemId">Parent task item (mutually exclusive with other parent FKs).</param>
    /// <param name="personId">Parent person (mutually exclusive with other parent FKs).</param>
    /// <param name="fileName">Original file name including extension.</param>
    /// <param name="contentType">MIME content type of the uploaded file.</param>
    /// <param name="fileStream">The uploaded file content stream.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created attachment entity with populated metadata.</returns>
    /// <exception cref="InputValidationException">
    /// Content type is not allowed, file exceeds size limit, file name is empty, or exactly one parent is not specified.
    /// </exception>
    /// <exception cref="EntityNotFoundException">The specified parent entity does not exist.</exception>
    Task<Attachment> CreateAttachmentAsync(
        Guid tenantId,
        Guid? calendarEventId,
        Guid? taskItemId,
        Guid? personId,
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves attachment metadata by its unique identifier.
    /// </summary>
    /// <param name="attachmentId">The unique identifier of the attachment.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The attachment if found; otherwise, <c>null</c>.</returns>
    Task<Attachment?> GetAttachmentAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a read stream for the attachment's file content.
    /// </summary>
    /// <param name="attachmentId">The unique identifier of the attachment.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A tuple of the attachment metadata and the file content stream.</returns>
    /// <exception cref="EntityNotFoundException">The attachment does not exist.</exception>
    Task<(Attachment Attachment, Stream FileStream)> GetAttachmentStreamAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an attachment and removes the physical file from storage.
    /// </summary>
    /// <param name="attachmentId">The unique identifier of the attachment to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the attachment was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteAttachmentAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}
