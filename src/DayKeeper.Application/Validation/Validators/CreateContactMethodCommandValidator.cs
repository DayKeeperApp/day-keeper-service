using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="CreateContactMethodCommand"/> input before contact method creation.
/// </summary>
public sealed class CreateContactMethodCommandValidator : AbstractValidator<CreateContactMethodCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>PersonId</c> required; <c>Type</c> valid enum;
    /// <c>Value</c> required, max 512; <c>Label</c> max 128 when provided.
    /// </summary>
    public CreateContactMethodCommandValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.Label)
            .MaximumLength(128)
            .When(x => x.Label is not null);
    }
}
