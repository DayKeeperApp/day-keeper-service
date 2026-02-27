using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new();

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var command = new CreateTenantCommand("Test Org", "test-org");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidInput_WithSingleSegmentSlug_PassesValidation()
    {
        var command = new CreateTenantCommand("Acme", "acme123");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidInput_WithMultiSegmentSlug_PassesValidation()
    {
        var command = new CreateTenantCommand("My Company", "my-company-2024");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Name: NotEmpty ────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Name_Empty_FailsValidation(string? name)
    {
        var command = new CreateTenantCommand(name!, "valid-slug");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── Name: MaximumLength(256) ──────────────────────────────────────

    [Fact]
    public async Task Name_AtMaxLength_PassesValidation()
    {
        var command = new CreateTenantCommand(new string('a', 256), "valid-slug");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_ExceedsMaxLength_FailsValidation()
    {
        var command = new CreateTenantCommand(new string('a', 257), "valid-slug");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── Slug: NotEmpty ────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Slug_Empty_FailsValidation(string? slug)
    {
        var command = new CreateTenantCommand("Valid Name", slug!);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    // ── Slug: MaximumLength(128) ──────────────────────────────────────

    [Fact]
    public async Task Slug_AtMaxLength_PassesValidation()
    {
        // 128-char valid slug: two 63-char lowercase segments joined by a hyphen
        var slug = new string('a', 63) + "-" + new string('b', 63);
        var command = new CreateTenantCommand("Valid Name", slug);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Slug_ExceedsMaxLength_FailsValidation()
    {
        var slug = new string('a', 129);
        var command = new CreateTenantCommand("Valid Name", slug);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    // ── Slug: Matches regex ───────────────────────────────────────────

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
        var command = new CreateTenantCommand("Valid Name", slug);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }
}
