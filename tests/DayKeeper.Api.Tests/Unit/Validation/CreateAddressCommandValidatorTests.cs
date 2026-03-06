using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreateAddressCommandValidatorTests
{
    private readonly CreateAddressCommandValidator _validator = new();

    private static CreateAddressCommand ValidCommand() =>
        new(Guid.NewGuid(), null, "123 Main St", null, "Springfield", null, null, "US", false);

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

    // ── Street1: NotEmpty, MaximumLength(512) ────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Street1_Empty_FailsValidation(string? street1)
    {
        var command = ValidCommand() with { Street1 = street1! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Street1");
    }

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

    // ── City: NotEmpty, MaximumLength(256) ───────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task City_Empty_FailsValidation(string? city)
    {
        var command = ValidCommand() with { City = city! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "City");
    }

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

    // ── Country: NotEmpty, MaximumLength(128) ────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Country_Empty_FailsValidation(string? country)
    {
        var command = ValidCommand() with { Country = country! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

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
}
