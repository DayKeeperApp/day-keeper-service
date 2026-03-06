using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreatePersonCommandValidatorTests
{
    private readonly CreatePersonCommandValidator _validator = new();

    private static CreatePersonCommand ValidCommand() =>
        new(Guid.NewGuid(), "John", "Doe", null);

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

    // ── FirstName: NotEmpty ──────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FirstName_Empty_FailsValidation(string? firstName)
    {
        var command = ValidCommand() with { FirstName = firstName! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    // ── FirstName: MaximumLength(256) ────────────────────────────────

    [Fact]
    public async Task FirstName_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { FirstName = new string('a', 256) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FirstName_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { FirstName = new string('a', 257) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    // ── LastName: NotEmpty ───────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LastName_Empty_FailsValidation(string? lastName)
    {
        var command = ValidCommand() with { LastName = lastName! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    // ── LastName: MaximumLength(256) ─────────────────────────────────

    [Fact]
    public async Task LastName_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { LastName = new string('a', 256) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task LastName_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { LastName = new string('a', 257) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    // ── Notes: MaximumLength(4000) when provided ─────────────────────

    [Fact]
    public async Task Notes_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Notes = new string('a', 4000) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Notes_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Notes = new string('a', 4001) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public async Task Notes_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Notes = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
