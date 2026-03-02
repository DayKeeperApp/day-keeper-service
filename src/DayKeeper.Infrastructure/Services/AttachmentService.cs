using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IAttachmentService"/>.
/// Orchestrates file storage operations and database persistence
/// for attachment management.
/// </summary>
public sealed class AttachmentService(
    IRepository<Attachment> attachmentRepository,
    IRepository<CalendarEvent> calendarEventRepository,
    IRepository<TaskItem> taskItemRepository,
    IRepository<Person> personRepository,
    IAttachmentStorageService storageService,
    IConfiguration configuration) : IAttachmentService
{
    private readonly IRepository<Attachment> _attachmentRepository = attachmentRepository;
    private readonly IRepository<CalendarEvent> _calendarEventRepository = calendarEventRepository;
    private readonly IRepository<TaskItem> _taskItemRepository = taskItemRepository;
    private readonly IRepository<Person> _personRepository = personRepository;
    private readonly IAttachmentStorageService _storageService = storageService;
    private readonly long _maxFileSizeBytes =
        long.TryParse(configuration["AttachmentStorage:MaxFileSizeBytes"], System.Globalization.CultureInfo.InvariantCulture, out var max) ? max : 10 * 1024 * 1024;

    /// <inheritdoc />
    public async Task<Attachment> CreateAttachmentAsync(
        Guid tenantId,
        Guid? calendarEventId,
        Guid? taskItemId,
        Guid? personId,
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        ValidateSingleParent(calendarEventId, taskItemId, personId);
        ValidateFileName(fileName);
        ValidateContentType(contentType);
        await ValidateParentExistsAsync(calendarEventId, taskItemId, personId, cancellationToken)
            .ConfigureAwait(false);

        // Buffer to MemoryStream to measure size (max 10 MB, safe for memory)
        var buffer = new MemoryStream();
        await using (buffer.ConfigureAwait(false))
        {
            await fileStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (buffer.Length > _maxFileSizeBytes)
            {
                throw new InputValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["file"] = [$"File size ({buffer.Length} bytes) exceeds maximum allowed size ({_maxFileSizeBytes} bytes)."],
                });
            }

            var attachmentId = Guid.NewGuid();
            var parentType = GetParentType(calendarEventId, taskItemId, personId);
            var parentId = (calendarEventId ?? taskItemId ?? personId)!.Value;
            var sanitizedFileName = Path.GetFileName(fileName.Trim());
            var storagePath = $"{tenantId}/{parentType}/{parentId}/{attachmentId}/{sanitizedFileName}";

            // Save file first
            buffer.Position = 0;
            await _storageService.SaveAsync(storagePath, buffer, cancellationToken)
                .ConfigureAwait(false);

            // Create entity; clean up file on failure
            try
            {
                var attachment = new Attachment
                {
                    Id = attachmentId,
                    TenantId = tenantId,
                    CalendarEventId = calendarEventId,
                    TaskItemId = taskItemId,
                    PersonId = personId,
                    FileName = sanitizedFileName,
                    ContentType = contentType,
                    FileSize = buffer.Length,
                    StoragePath = storagePath,
                };

                return await _attachmentRepository.AddAsync(attachment, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Best-effort cleanup of the saved file
                await _storageService.DeleteAsync(storagePath, cancellationToken)
                    .ConfigureAwait(false);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<Attachment?> GetAttachmentAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        return await _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(Attachment Attachment, Stream FileStream)> GetAttachmentStreamAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Attachment), attachmentId);

        var stream = await _storageService.ReadAsync(attachment.StoragePath, cancellationToken)
            .ConfigureAwait(false);

        return (attachment, stream);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAttachmentAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken)
            .ConfigureAwait(false);

        if (attachment is null)
        {
            return false;
        }

        // Soft-delete the entity
        await _attachmentRepository.DeleteAsync(attachmentId, cancellationToken)
            .ConfigureAwait(false);

        // Physically delete the file (best-effort)
        await _storageService.DeleteAsync(attachment.StoragePath, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    // ── Validation helpers ────────────────────────────────────

    private static void ValidateSingleParent(Guid? calendarEventId, Guid? taskItemId, Guid? personId)
    {
        var count =
            (calendarEventId.HasValue ? 1 : 0)
            + (taskItemId.HasValue ? 1 : 0)
            + (personId.HasValue ? 1 : 0);

        if (count != 1)
        {
            throw new InputValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["parentId"] = ["Exactly one parent entity (calendarEventId, taskItemId, or personId) must be specified."],
            });
        }
    }

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName.Trim()).Length == 0)
        {
            throw new InputValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["fileName"] = ["File name is required."],
            });
        }

        if (fileName.Trim().Length > 512)
        {
            throw new InputValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["fileName"] = ["File name must not exceed 512 characters."],
            });
        }
    }

    private static void ValidateContentType(string contentType)
    {
        if (!Attachment.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InputValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["contentType"] = [$"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", Attachment.AllowedContentTypes.Order(StringComparer.Ordinal))}."],
            });
        }
    }

    private async Task ValidateParentExistsAsync(
        Guid? calendarEventId,
        Guid? taskItemId,
        Guid? personId,
        CancellationToken cancellationToken)
    {
        if (calendarEventId.HasValue)
        {
            _ = await _calendarEventRepository.GetByIdAsync(calendarEventId.Value, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new EntityNotFoundException(nameof(CalendarEvent), calendarEventId.Value);
        }
        else if (taskItemId.HasValue)
        {
            _ = await _taskItemRepository.GetByIdAsync(taskItemId.Value, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new EntityNotFoundException(nameof(TaskItem), taskItemId.Value);
        }
        else if (personId.HasValue)
        {
            _ = await _personRepository.GetByIdAsync(personId.Value, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new EntityNotFoundException(nameof(Person), personId.Value);
        }
    }

    private static string GetParentType(Guid? calendarEventId, Guid? taskItemId, Guid? personId)
    {
        if (calendarEventId.HasValue) return "calendarEvent";
        if (taskItemId.HasValue) return "taskItem";
        return "person";
    }
}
