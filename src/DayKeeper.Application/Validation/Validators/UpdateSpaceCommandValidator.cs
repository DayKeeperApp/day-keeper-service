using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class UpdateSpaceCommandValidator : AbstractValidator<UpdateSpaceCommand>
{
    public UpdateSpaceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(256)
            .When(x => x.Name is not null);

        RuleFor(x => x.SpaceType)
            .IsInEnum()
            .When(x => x.SpaceType is not null);
    }
}
