using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdateAddressCommand"/> input before address updates.
/// </summary>
public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>Id</c> required; all optional fields have
    /// max length constraints when provided.
    /// </summary>
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Label)
            .MaximumLength(128)
            .When(x => x.Label is not null);

        RuleFor(x => x.Street1)
            .MaximumLength(512)
            .When(x => x.Street1 is not null);

        RuleFor(x => x.Street2)
            .MaximumLength(512)
            .When(x => x.Street2 is not null);

        RuleFor(x => x.City)
            .MaximumLength(256)
            .When(x => x.City is not null);

        RuleFor(x => x.State)
            .MaximumLength(128)
            .When(x => x.State is not null);

        RuleFor(x => x.PostalCode)
            .MaximumLength(32)
            .When(x => x.PostalCode is not null);

        RuleFor(x => x.Country)
            .MaximumLength(128)
            .When(x => x.Country is not null);
    }
}
