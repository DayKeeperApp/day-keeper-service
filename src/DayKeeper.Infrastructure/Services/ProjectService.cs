using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IProjectService"/>.
/// Orchestrates business rules for project management
/// using repository abstractions and direct DbContext queries.
/// </summary>
public sealed class ProjectService(
    IRepository<Project> projectRepository,
    IRepository<Space> spaceRepository,
    DbContext dbContext) : IProjectService
{
    private readonly IRepository<Project> _projectRepository = projectRepository;
    private readonly IRepository<Space> _spaceRepository = spaceRepository;
    private readonly DbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Project> CreateProjectAsync(
        Guid spaceId,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        _ = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Space), spaceId);

        var normalizedName = name.Trim().ToLowerInvariant();

        var nameExists = await _dbContext.Set<Project>()
            .AnyAsync(p => p.SpaceId == spaceId && p.NormalizedName == normalizedName,
                cancellationToken)
            .ConfigureAwait(false);

        if (nameExists)
        {
            throw new DuplicateProjectNameException(spaceId, normalizedName);
        }

        var project = new Project
        {
            SpaceId = spaceId,
            Name = name.Trim(),
            NormalizedName = normalizedName,
            Description = description,
        };

        return await _projectRepository.AddAsync(project, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Project?> GetProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Project> UpdateProjectAsync(
        Guid projectId,
        string? name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        if (name is not null)
        {
            var normalizedName = name.Trim().ToLowerInvariant();

            if (!string.Equals(normalizedName, project.NormalizedName, StringComparison.Ordinal))
            {
                var nameExists = await _dbContext.Set<Project>()
                    .AnyAsync(p => p.SpaceId == project.SpaceId
                                && p.NormalizedName == normalizedName
                                && p.Id != projectId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (nameExists)
                {
                    throw new DuplicateProjectNameException(project.SpaceId, normalizedName);
                }
            }

            project.Name = name.Trim();
            project.NormalizedName = normalizedName;
        }

        if (description is not null)
        {
            project.Description = description;
        }

        await _projectRepository.UpdateAsync(project, cancellationToken)
            .ConfigureAwait(false);

        return project;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _projectRepository.DeleteAsync(projectId, cancellationToken)
            .ConfigureAwait(false);
    }
}
