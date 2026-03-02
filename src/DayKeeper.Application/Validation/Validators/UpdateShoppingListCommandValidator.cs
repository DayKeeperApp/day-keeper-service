using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class UpdateShoppingListCommandValidator : AbstractValidator<UpdateShoppingListCommand>
{
    public UpdateShoppingListCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(256).When(x => x.Name is not null);
    }
}
