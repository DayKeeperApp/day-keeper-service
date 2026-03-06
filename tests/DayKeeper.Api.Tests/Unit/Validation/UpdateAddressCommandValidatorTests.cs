using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateAddressCommandValidatorTests
{
    private readonly UpdateAddressCommandValidator _validator = new();

    private static UpdateAddressCommand ValidCommand() =>
        new(Guid.NewGuid(), null, "123 Main St", null, "Springfield", null, null, "US", false);

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

    // ── Label: MaximumLength(128) when provided ─────────────────────

    [Fact]
    public async Task Label_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Label = new string('a', 128) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Label_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Label = new string('a', 129) };

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

    // ── Street1: MaximumLength(512) when provided ────────────────────

    [Fact]
    public async Task Street1_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Street1 = new string('a', 512) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Street1_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Street1 = new string('a', 513) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Street1");
    }

    [Fact]
    public async Task Street1_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Street1 = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Street2: MaximumLength(512) when provided ────────────────────

    [Fact]
    public async Task Street2_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Street2 = new string('a', 512) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Street2_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Street2 = new string('a', 513) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Street2");
    }

    [Fact]
    public async Task Street2_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Street2 = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── City: MaximumLength(256) when provided ──────────────────────

    [Fact]
    public async Task City_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { City = new string('a', 256) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task City_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { City = new string('a', 257) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "City");
    }

    [Fact]
    public async Task City_Null_SkipsValidation()
    {
        var command = ValidCommand() with { City = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── State: MaximumLength(128) when provided ─────────────────────

    [Fact]
    public async Task State_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { State = new string('a', 128) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task State_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { State = new string('a', 129) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "State");
    }

    [Fact]
    public async Task State_Null_SkipsValidation()
    {
        var command = ValidCommand() with { State = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── PostalCode: MaximumLength(32) when provided ─────────────────

    [Fact]
    public async Task PostalCode_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { PostalCode = new string('a', 32) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PostalCode_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { PostalCode = new string('a', 33) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode");
    }

    [Fact]
    public async Task PostalCode_Null_SkipsValidation()
    {
        var command = ValidCommand() with { PostalCode = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Country: MaximumLength(128) when provided ───────────────────

    [Fact]
    public async Task Country_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Country = new string('a', 128) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Country_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Country = new string('a', 129) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Fact]
    public async Task Country_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Country = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
