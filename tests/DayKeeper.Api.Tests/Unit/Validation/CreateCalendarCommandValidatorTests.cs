using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreateCalendarCommandValidatorTests
{
    private readonly CreateCalendarCommandValidator _validator = new();

    private static CreateCalendarCommand ValidCommand() =>
        new(Guid.NewGuid(), "My Calendar", "#4A90D9", false);

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    // ── SpaceId: NotEmpty ────────────────────────────────────────────

    [Fact]
    public async Task SpaceId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { SpaceId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SpaceId");
    }

    // ── Name: NotEmpty ──────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Name_Empty_FailsValidation(string? name)
    {
        var command = ValidCommand() with { Name = name! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── Name: MaximumLength(256) ────────────────────────────────────

    [Fact]
    public async Task Name_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Name = new string('a', 256) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Name = new string('a', 257) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── Color: NotEmpty ─────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Color_Empty_FailsValidation(string? color)
    {
        var command = ValidCommand() with { Color = color! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Color");
    }

    // ── Color: MaximumLength(16) ────────────────────────────────────

    [Fact]
    public async Task Color_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Color = new string('a', 16) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Color_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Color = new string('a', 17) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Color");
    }
}
