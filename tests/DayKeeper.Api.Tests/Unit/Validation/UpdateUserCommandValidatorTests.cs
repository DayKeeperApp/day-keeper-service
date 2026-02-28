using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    private static UpdateUserCommand ValidCommand() =>
        new(Guid.NewGuid(), "Jane Doe", "jane@example.com", "America/New_York", WeekStart.Monday, "en-US");

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_AllFieldsProvided_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidInput_AllOptionalFieldsNull_PassesValidation()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Id: NotEmpty ──────────────────────────────────────────────────

    [Fact]
    public async Task Id_Empty_FailsValidation()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    // ── DisplayName: MaximumLength(256) when not null ─────────────────

    [Fact]
    public async Task DisplayName_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { DisplayName = new string('a', 256) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DisplayName_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { DisplayName = new string('a', 257) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    [Fact]
    public async Task DisplayName_Null_SkipsValidation()
    {
        var command = ValidCommand() with { DisplayName = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "DisplayName");
    }

    // ── Email: MaximumLength(320) when not null ───────────────────────

    [Fact]
    public async Task Email_ExceedsMaxLength_FailsValidation()
    {
        var local = new string('a', 310);
        var command = ValidCommand() with { Email = $"{local}@example.com" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    // ── Email: EmailAddress when not null ─────────────────────────────

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain")]
    [InlineData("no-at-sign")]
    public async Task Email_InvalidFormat_FailsValidation(string email)
    {
        var command = ValidCommand() with { Email = email };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Email_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Email = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "Email");
    }

    // ── Timezone: MaximumLength(64) when not null ─────────────────────

    [Fact]
    public async Task Timezone_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Timezone = new string('a', 65) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone");
    }

    // ── Timezone: Must(BeValidIanaTimezone) when not null ─────────────

    [Fact]
    public async Task Timezone_ValidIana_PassesValidation()
    {
        var command = ValidCommand() with { Timezone = "America/New_York" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Timezone_InvalidIana_FailsValidation()
    {
        var command = ValidCommand() with { Timezone = "Invalid/Zone" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone");
    }

    [Fact]
    public async Task Timezone_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Timezone = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "Timezone");
    }

    // ── WeekStart: IsInEnum when not null ─────────────────────────────

    [Fact]
    public async Task WeekStart_InvalidEnumValue_FailsValidation()
    {
        var command = ValidCommand() with { WeekStart = (WeekStart)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WeekStart");
    }

    [Fact]
    public async Task WeekStart_Null_SkipsValidation()
    {
        var command = ValidCommand() with { WeekStart = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "WeekStart");
    }

    // ── Locale: MaximumLength(16) when not null ───────────────────────

    [Fact]
    public async Task Locale_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Locale = new string('a', 16) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Locale_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Locale = new string('a', 17) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Locale");
    }

    [Fact]
    public async Task Locale_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Locale = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "Locale");
    }
}
