using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="AddSpaceMemberCommand"/> input before adding a space member.
/// </summary>
public sealed class AddSpaceMemberCommandValidator : AbstractValidator<AddSpaceMemberCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>SpaceId</c> required; <c>UserId</c> required;
    /// <c>Role</c> valid enum.
    /// </summary>
    public AddSpaceMemberCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .IsInEnum();
    }
}
