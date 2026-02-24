namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Abstraction for PVC-backed file storage operations for attachments.
/// </summary>
public interface IAttachmentStorageService
{
    /// <summary>
    /// Saves a file to the attachment storage volume.
    /// </summary>
    /// <param name="storagePath">The relative path within the storage volume where the file will be saved.</param>
    /// <param name="content">The file content stream.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task SaveAsync(string storagePath, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a read-only stream to a file in the attachment storage volume.
    /// </summary>
    /// <param name="storagePath">The relative path within the storage volume to the file.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A read-only stream to the file content.</returns>
    Task<Stream> ReadAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the attachment storage volume.
    /// </summary>
    /// <param name="storagePath">The relative path within the storage volume to the file.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the file was found and deleted; <c>false</c> if the file did not exist.</returns>
    Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
