using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
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
