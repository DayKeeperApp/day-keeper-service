using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing projects within a space.
/// Orchestrates business rules, validation, and persistence for
/// <see cref="Project"/> entities.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Creates a new project within the specified space.
    /// </summary>
    /// <param name="spaceId">The space under which to create the project.</param>
    /// <param name="name">The display name for the project.</param>
    /// <param name="description">Optional description for the project.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created project.</returns>
    /// <exception cref="EntityNotFoundException">The specified space does not exist.</exception>
    /// <exception cref="DuplicateProjectNameException">A project with the same normalized name already exists in this space.</exception>
    Task<Project> CreateProjectAsync(
        Guid spaceId,
        string name,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a project by its unique identifier.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The project if found; otherwise, <c>null</c>.</returns>
    Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the name and/or description of an existing project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project to update.</param>
    /// <param name="name">The new display name, or <c>null</c> to leave unchanged.</param>
    /// <param name="description">The new description, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated project.</returns>
    /// <exception cref="EntityNotFoundException">The project does not exist.</exception>
    /// <exception cref="DuplicateProjectNameException">The new name conflicts with an existing project in the same space.</exception>
    Task<Project> UpdateProjectAsync(
        Guid projectId,
        string? name,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a project.
    /// </summary>
    /// <param name="projectId">The unique identifier of the project to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the project was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}
