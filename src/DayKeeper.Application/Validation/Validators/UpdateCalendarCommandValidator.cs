using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdateCalendarCommand"/> input before calendar updates.
/// </summary>
public sealed class UpdateCalendarCommandValidator : AbstractValidator<UpdateCalendarCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>Id</c> required; <c>Name</c> max 256 when provided;
    /// <c>Color</c> max 16 when provided.
    /// </summary>
    public UpdateCalendarCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(256)
            .When(x => x.Name is not null);

        RuleFor(x => x.Color)
            .MaximumLength(16)
            .When(x => x.Color is not null);
    }
}
