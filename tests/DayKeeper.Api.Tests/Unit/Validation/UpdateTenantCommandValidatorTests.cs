using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateTenantCommandValidatorTests
{
    private readonly UpdateTenantCommandValidator _validator = new();

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_AllFieldsProvided_PassesValidation()
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), "Updated Org", "updated-org");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidInput_OptionalFieldsNull_PassesValidation()
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Id: NotEmpty ──────────────────────────────────────────────────

    [Fact]
    public async Task Id_Empty_FailsValidation()
    {
        var command = new UpdateTenantCommand(Guid.Empty, "Valid Name", "valid-slug");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    // ── Name: MaximumLength(256) when not null ────────────────────────

    [Fact]
    public async Task Name_AtMaxLength_PassesValidation()
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), new string('a', 256), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_ExceedsMaxLength_FailsValidation()
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), new string('a', 257), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_Null_SkipsLengthValidation()
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "Name");
    }

    // ── Slug: MaximumLength(128) when not null ────────────────────────

    [Fact]
    public async Task Slug_ExceedsMaxLength_FailsValidation()
    {
        var slug = new string('a', 129);
        var command = new UpdateTenantCommand(Guid.NewGuid(), null, slug);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    [Fact]
    public async Task Slug_Null_SkipsValidation()
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "Slug");
    }

    // ── Slug: Matches regex when not null ─────────────────────────────

    [Theory]
    [InlineData("UPPERCASE")]
    [InlineData("has spaces")]
    [InlineData("has_underscore")]
    [InlineData("-leading-hyphen")]
    [InlineData("trailing-hyphen-")]
    [InlineData("double--hyphen")]
    [InlineData("has.dot")]
    public async Task Slug_InvalidFormat_FailsValidation(string slug)
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), null, slug);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }
}
