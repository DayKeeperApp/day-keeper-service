using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="CreateSpaceCommand"/> input before space creation.
/// </summary>
public sealed class CreateSpaceCommandValidator : AbstractValidator<CreateSpaceCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>TenantId</c> required; <c>Name</c> required, max 256;
    /// <c>SpaceType</c> valid enum; <c>CreatedByUserId</c> required.
    /// </summary>
    public CreateSpaceCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.SpaceType)
            .IsInEnum();

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty();
    }
}
