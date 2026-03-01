using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="CreateAddressCommand"/> input before address creation.
/// </summary>
public sealed class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>PersonId</c> required; <c>Street1</c> required, max 512;
    /// <c>City</c> required, max 256; <c>Country</c> required, max 128;
    /// optional fields have max length constraints when provided.
    /// </summary>
    public CreateAddressCommandValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty();

        RuleFor(x => x.Label)
            .MaximumLength(128)
            .When(x => x.Label is not null);

        RuleFor(x => x.Street1)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.Street2)
            .MaximumLength(512)
            .When(x => x.Street2 is not null);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.State)
            .MaximumLength(128)
            .When(x => x.State is not null);

        RuleFor(x => x.PostalCode)
            .MaximumLength(32)
            .When(x => x.PostalCode is not null);

        RuleFor(x => x.Country)
            .NotEmpty()
            .MaximumLength(128);
    }
}
