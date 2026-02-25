namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when attempting to add a membership that already exists
/// (same space and user combination).
/// </summary>
public sealed class DuplicateMembershipException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateMembershipException"/> class.
    /// </summary>
    /// <param name="spaceId">The space where the duplicate was detected.</param>
    /// <param name="userId">The user that is already a member.</param>
    public DuplicateMembershipException(Guid spaceId, Guid userId)
        : base($"User '{userId}' is already a member of space '{spaceId}'.")
    {
        SpaceId = spaceId;
        UserId = userId;
    }

    /// <summary>The space where the duplicate was detected.</summary>
    public Guid SpaceId { get; }

    /// <summary>The user that is already a member.</summary>
    public Guid UserId { get; }
}
