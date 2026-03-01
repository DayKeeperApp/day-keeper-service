using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdateImportantDateCommand"/> input before important date updates.
/// </summary>
public sealed class UpdateImportantDateCommandValidator : AbstractValidator<UpdateImportantDateCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>Id</c> required; <c>Label</c> max 256 when provided.
    /// </summary>
    public UpdateImportantDateCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Label)
            .MaximumLength(256)
            .When(x => x.Label is not null);
    }
}
