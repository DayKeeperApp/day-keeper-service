using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="Project"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class ProjectMutations
{
    /// <summary>Creates a new project within a space.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateProjectNameException>]
    public Task<Project> CreateProjectAsync(
        Guid spaceId,
        string name,
        string? description,
        IProjectService projectService,
        CancellationToken cancellationToken)
    {
        return projectService.CreateProjectAsync(
            spaceId, name, description, cancellationToken);
    }

    /// <summary>Updates an existing project.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateProjectNameException>]
    public Task<Project> UpdateProjectAsync(
        Guid id,
        string? name,
        string? description,
        IProjectService projectService,
        CancellationToken cancellationToken)
    {
        return projectService.UpdateProjectAsync(
            id, name, description, cancellationToken);
    }

    /// <summary>Soft-deletes a project.</summary>
    public Task<bool> DeleteProjectAsync(
        Guid id,
        IProjectService projectService,
        CancellationToken cancellationToken)
    {
        return projectService.DeleteProjectAsync(id, cancellationToken);
    }
}
