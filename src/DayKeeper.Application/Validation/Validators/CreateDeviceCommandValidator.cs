using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class CreateDeviceCommandValidator : AbstractValidator<CreateDeviceCommand>
{
    public CreateDeviceCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DeviceName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.FcmToken).NotEmpty().MaximumLength(4096);
    }
}
