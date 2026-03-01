using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdatePersonCommand"/> input before person updates.
/// </summary>
public sealed class UpdatePersonCommandValidator : AbstractValidator<UpdatePersonCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>Id</c> required; <c>FirstName</c> max 256 when provided;
    /// <c>LastName</c> max 256 when provided; <c>Notes</c> max 4000 when provided.
    /// </summary>
    public UpdatePersonCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.FirstName)
            .MaximumLength(256)
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(256)
            .When(x => x.LastName is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .When(x => x.Notes is not null);
    }
}
