using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdatePersonCommandValidatorTests
{
    private readonly UpdatePersonCommandValidator _validator = new();

    private static UpdatePersonCommand ValidCommand() =>
        new(Guid.NewGuid(), "John", "Doe", null);

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

    // ── FirstName: MaximumLength(256) when provided ──────────────────

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

    [Fact]
    public async Task FirstName_Null_SkipsValidation()
    {
        var command = ValidCommand() with { FirstName = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── LastName: MaximumLength(256) when provided ───────────────────

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

    [Fact]
    public async Task LastName_Null_SkipsValidation()
    {
        var command = ValidCommand() with { LastName = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
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
