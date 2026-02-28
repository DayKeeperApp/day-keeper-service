using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdateTenantCommand"/> input before tenant updates.
/// </summary>
public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>Id</c> required; <c>Name</c> max 256 when provided;
    /// <c>Slug</c> max 128, lowercase alphanumeric with hyphens when provided.
    /// </summary>
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(256)
            .When(x => x.Name is not null);

        RuleFor(x => x.Slug)
            .MaximumLength(128)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.")
            .When(x => x.Slug is not null);
    }
}
