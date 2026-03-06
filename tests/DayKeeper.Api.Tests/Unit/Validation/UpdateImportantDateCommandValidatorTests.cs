using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateImportantDateCommandValidatorTests
{
    private readonly UpdateImportantDateCommandValidator _validator = new();

    private static UpdateImportantDateCommand ValidCommand() =>
        new(Guid.NewGuid(), "Birthday", new DateOnly(1990, 6, 15), null);

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    // ── Id: NotEmpty ─────────────────────────────────────────────────

    [Fact]
    public async Task Id_Empty_FailsValidation()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    // ── Label: MaximumLength(256) when provided ─────────────────────

    [Fact]
    public async Task Label_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Label = new string('a', 256) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Label_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Label = new string('a', 257) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Label");
    }

    [Fact]
    public async Task Label_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Label = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
