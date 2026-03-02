using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="Attachment"/> entities.
/// File upload is handled by the REST endpoint; this mutation
/// provides GraphQL-native deletion.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class AttachmentMutations
{
    /// <summary>Soft-deletes an attachment and removes its physical file from storage.</summary>
    public Task<bool> DeleteAttachmentAsync(
        Guid id,
        IAttachmentService attachmentService,
        CancellationToken cancellationToken)
    {
        return attachmentService.DeleteAttachmentAsync(id, cancellationToken);
    }
}
