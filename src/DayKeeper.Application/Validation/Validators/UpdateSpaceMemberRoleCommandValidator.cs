using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdateSpaceMemberRoleCommand"/> input before updating a member's role.
/// </summary>
public sealed class UpdateSpaceMemberRoleCommandValidator : AbstractValidator<UpdateSpaceMemberRoleCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>SpaceId</c> required; <c>UserId</c> required;
    /// <c>NewRole</c> valid enum.
    /// </summary>
    public UpdateSpaceMemberRoleCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.NewRole)
            .IsInEnum();
    }
}
