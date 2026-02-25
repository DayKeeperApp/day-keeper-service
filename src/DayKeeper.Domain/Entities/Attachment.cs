using DayKeeper.Domain.Interfaces;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// A file attachment associated with a <see cref="CalendarEvent"/>,
/// <see cref="TaskItem"/>, or <see cref="Person"/>.
/// Exactly one of the parent foreign keys must be populated.
/// </summary>
public class Attachment : BaseEntity, ITenantScoped
{
    /// <summary>Foreign key to the owning <see cref="Tenant"/>.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Foreign key to the associated <see cref="CalendarEvent"/>.
    /// <c>null</c> if the attachment is not associated with an event.
    /// </summary>
    public Guid? CalendarEventId { get; set; }

    /// <summary>
    /// Foreign key to the associated <see cref="TaskItem"/>.
    /// <c>null</c> if the attachment is not associated with a task.
    /// </summary>
    public Guid? TaskItemId { get; set; }

    /// <summary>
    /// Foreign key to the associated <see cref="Person"/>.
    /// <c>null</c> if the attachment is not associated with a person.
    /// </summary>
    public Guid? PersonId { get; set; }

    /// <summary>Original file name including extension.</summary>
    public required string FileName { get; set; }

    /// <summary>
    /// MIME content type of the file (e.g. "image/jpeg", "application/pdf").
    /// Must be one of the values in <see cref="AllowedContentTypes"/>.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>File size in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Relative path to the file within the attachment storage volume.</summary>
    public required string StoragePath { get; set; }

    /// <summary>Navigation to the owning tenant.</summary>
    public Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Navigation to the associated event.
    /// <c>null</c> if the attachment is not associated with an event.
    /// </summary>
    public CalendarEvent? CalendarEvent { get; set; }

    /// <summary>
    /// Navigation to the associated task.
    /// <c>null</c> if the attachment is not associated with a task.
    /// </summary>
    public TaskItem? TaskItem { get; set; }

    /// <summary>
    /// Navigation to the associated person.
    /// <c>null</c> if the attachment is not associated with a person.
    /// </summary>
    public Person? Person { get; set; }

    /// <summary>
    /// The set of MIME content types allowed for attachments.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic",
        "application/pdf",
    };
}
