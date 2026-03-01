using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdateContactMethodCommand"/> input before contact method updates.
/// </summary>
public sealed class UpdateContactMethodCommandValidator : AbstractValidator<UpdateContactMethodCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>Id</c> required; <c>Type</c> valid enum when provided;
    /// <c>Value</c> max 512 when provided; <c>Label</c> max 128 when provided.
    /// </summary>
    public UpdateContactMethodCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type is not null);

        RuleFor(x => x.Value)
            .MaximumLength(512)
            .When(x => x.Value is not null);

        RuleFor(x => x.Label)
            .MaximumLength(128)
            .When(x => x.Label is not null);
    }
}
