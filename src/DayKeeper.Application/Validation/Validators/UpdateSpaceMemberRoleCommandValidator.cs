using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class UpdateSpaceMemberRoleCommandValidator : AbstractValidator<UpdateSpaceMemberRoleCommand>
{
    public UpdateSpaceMemberRoleCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.NewRole)
            .IsInEnum();
    }
}
