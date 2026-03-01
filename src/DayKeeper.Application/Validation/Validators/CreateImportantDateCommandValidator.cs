using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="CreateImportantDateCommand"/> input before important date creation.
/// </summary>
public sealed class CreateImportantDateCommandValidator : AbstractValidator<CreateImportantDateCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>PersonId</c> required; <c>Label</c> required, max 256;
    /// <c>Date</c> required.
    /// </summary>
    public CreateImportantDateCommandValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty();

        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.DateValue)
            .NotEmpty();
    }
}
