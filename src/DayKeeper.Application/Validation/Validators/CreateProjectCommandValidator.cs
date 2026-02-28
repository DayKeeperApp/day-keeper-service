using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);
    }
}
