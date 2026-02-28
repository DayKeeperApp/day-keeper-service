using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdateSpaceCommand"/> input before space updates.
/// </summary>
public sealed class UpdateSpaceCommandValidator : AbstractValidator<UpdateSpaceCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>Id</c> required; <c>Name</c> max 256 when provided;
    /// <c>SpaceType</c> valid enum when provided.
    /// </summary>
    public UpdateSpaceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(256)
            .When(x => x.Name is not null);

        RuleFor(x => x.SpaceType)
            .IsInEnum()
            .When(x => x.SpaceType is not null);
    }
}
