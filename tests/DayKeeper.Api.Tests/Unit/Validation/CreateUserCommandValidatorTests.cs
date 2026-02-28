using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    private static CreateUserCommand ValidCommand() =>
        new(Guid.NewGuid(), "Jane Doe", "jane@example.com", "America/New_York", WeekStart.Monday, null);

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidInput_WithLocale_PassesValidation()
    {
        var command = ValidCommand() with { Locale = "en-US" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(WeekStart.Sunday)]
    [InlineData(WeekStart.Monday)]
    [InlineData(WeekStart.Saturday)]
    public async Task ValidInput_AllWeekStartValues_PassValidation(WeekStart weekStart)
    {
        var command = ValidCommand() with { WeekStart = weekStart };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── TenantId: NotEmpty ────────────────────────────────────────────

    [Fact]
    public async Task TenantId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { TenantId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    // ── DisplayName: NotEmpty ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DisplayName_Empty_FailsValidation(string? displayName)
    {
        var command = ValidCommand() with { DisplayName = displayName! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    // ── DisplayName: MaximumLength(256) ───────────────────────────────

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

    // ── Email: NotEmpty ───────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Email_Empty_FailsValidation(string? email)
    {
        var command = ValidCommand() with { Email = email! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    // ── Email: MaximumLength(320) ─────────────────────────────────────

    [Fact]
    public async Task Email_ExceedsMaxLength_FailsValidation()
    {
        // local-part@domain, total > 320
        var local = new string('a', 310);
        var command = ValidCommand() with { Email = $"{local}@example.com" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    // ── Email: EmailAddress ───────────────────────────────────────────

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

    // ── Timezone: NotEmpty ────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Timezone_Empty_FailsValidation(string timezone)
    {
        var command = ValidCommand() with { Timezone = timezone };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone");
    }

    [Fact]
    public async Task Timezone_Null_FailsValidation()
    {
        var command = ValidCommand() with { Timezone = null! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone");
    }

    // ── Timezone: MaximumLength(64) ───────────────────────────────────

    [Fact]
    public async Task Timezone_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Timezone = new string('a', 65) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone");
    }

    // ── Timezone: Must(BeValidIanaTimezone) ───────────────────────────

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

    // ── WeekStart: IsInEnum ───────────────────────────────────────────

    [Fact]
    public async Task WeekStart_InvalidEnumValue_FailsValidation()
    {
        var command = ValidCommand() with { WeekStart = (WeekStart)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WeekStart");
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
