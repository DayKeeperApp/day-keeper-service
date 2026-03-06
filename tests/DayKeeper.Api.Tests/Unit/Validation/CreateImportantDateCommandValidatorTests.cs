using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreateImportantDateCommandValidatorTests
{
    private readonly CreateImportantDateCommandValidator _validator = new();

    private static CreateImportantDateCommand ValidCommand() =>
        new(Guid.NewGuid(), "Birthday", new DateOnly(1990, 6, 15), null);

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    // ── PersonId: NotEmpty ───────────────────────────────────────────

    [Fact]
    public async Task PersonId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { PersonId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PersonId");
    }

    // ── Label: NotEmpty, MaximumLength(256) ──────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Label_Empty_FailsValidation(string? label)
    {
        var command = ValidCommand() with { Label = label! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Label");
    }

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

    // ── DateValue: NotEmpty ─────────────────────────────────────────

    [Fact]
    public async Task DateValue_Empty_FailsValidation()
    {
        var command = ValidCommand() with { DateValue = default };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DateValue");
    }
}
