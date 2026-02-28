using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="CreateCalendarCommand"/> input before calendar creation.
/// </summary>
public sealed class CreateCalendarCommandValidator : AbstractValidator<CreateCalendarCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>SpaceId</c> required; <c>Name</c> required, max 256;
    /// <c>Color</c> required, max 16.
    /// </summary>
    public CreateCalendarCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Color)
            .NotEmpty()
            .MaximumLength(16);
    }
}
