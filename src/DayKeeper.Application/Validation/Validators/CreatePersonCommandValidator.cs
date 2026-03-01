using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="CreatePersonCommand"/> input before person creation.
/// </summary>
public sealed class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>SpaceId</c> required; <c>FirstName</c> required, max 256;
    /// <c>LastName</c> required, max 256; <c>Notes</c> max 4000 when provided.
    /// </summary>
    public CreatePersonCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .When(x => x.Notes is not null);
    }
}
