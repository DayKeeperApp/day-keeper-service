using DayKeeper.Api.GraphQL;
using DayKeeper.Application.Exceptions;
using HotChocolate;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DayKeeper.Api.Tests.Unit.GraphQL;

public sealed class DomainErrorFilterTests
{
    private readonly ILogger<DomainErrorFilter> _logger = Substitute.For<ILogger<DomainErrorFilter>>();
    private readonly IHostEnvironment _env = Substitute.For<IHostEnvironment>();

    public DomainErrorFilterTests()
    {
        _env.EnvironmentName.Returns(Environments.Production);
    }

    private DomainErrorFilter CreateFilter() => new(_logger, _env);

    private static Error CreateErrorWithException(Exception ex) =>
        new(ex.Message, exception: ex);

    // ── EntityNotFoundException ────────────────────────────────

    [Fact]
    public void OnError_EntityNotFoundException_ReturnsNotFoundCode()
    {
        var ex = new EntityNotFoundException("Tenant", Guid.NewGuid());
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void OnError_EntityNotFoundException_SetsEntityNameExtension()
    {
        var ex = new EntityNotFoundException("Tenant", Guid.NewGuid());
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Extensions.Should().ContainKey("entityName")
            .WhoseValue.Should().Be("Tenant");
    }

    [Fact]
    public void OnError_EntityNotFoundException_WithGuidId_SetsEntityIdExtension()
    {
        var id = Guid.NewGuid();
        var ex = new EntityNotFoundException("Tenant", id);
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Extensions.Should().ContainKey("entityId")
            .WhoseValue.Should().Be(id);
    }

    [Fact]
    public void OnError_EntityNotFoundException_WithStringDescription_OmitsEntityIdExtension()
    {
        var ex = new EntityNotFoundException("Tenant", "slug 'foo'");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Extensions?.ContainsKey("entityId").Should().NotBe(true);
    }

    [Fact]
    public void OnError_EntityNotFoundException_PreservesOriginalMessage()
    {
        var id = Guid.NewGuid();
        var ex = new EntityNotFoundException("Tenant", id);
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Message.Should().Be($"Tenant with id '{id}' was not found.");
    }

    [Fact]
    public void OnError_EntityNotFoundException_RemovesException()
    {
        var ex = new EntityNotFoundException("Tenant", Guid.NewGuid());
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Exception.Should().BeNull();
    }

    // ── InputValidationException ───────────────────────────────

    [Fact]
    public void OnError_InputValidationException_ReturnsValidationErrorCode()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Name"] = ["Name is required."],
        };
        var ex = new InputValidationException(errors);
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public void OnError_InputValidationException_SetsPerFieldExtensions()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Name"] = ["Name is required."],
            ["Slug"] = ["Slug must match pattern.", "Slug is too long."],
        };
        var ex = new InputValidationException(errors);
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Extensions.Should().ContainKey("validation.Name");
        result.Extensions.Should().ContainKey("validation.Slug");
        ((string[])result.Extensions!["validation.Slug"]!).Should().HaveCount(2);
    }

    [Fact]
    public void OnError_InputValidationException_RemovesException()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Name"] = ["Name is required."],
        };
        var ex = new InputValidationException(errors);
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Exception.Should().BeNull();
    }

    // ── BusinessRuleViolationException ─────────────────────────

    [Fact]
    public void OnError_BusinessRuleViolationException_ReturnsBusinessRuleViolationCode()
    {
        var ex = new BusinessRuleViolationException("InsufficientSpaceRole", "User lacks required role.");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public void OnError_BusinessRuleViolationException_SetsRuleExtension()
    {
        var ex = new BusinessRuleViolationException("InsufficientSpaceRole", "User lacks required role.");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Extensions.Should().ContainKey("rule")
            .WhoseValue.Should().Be("InsufficientSpaceRole");
    }

    [Fact]
    public void OnError_BusinessRuleViolationException_PreservesMessage()
    {
        var ex = new BusinessRuleViolationException("InsufficientSpaceRole", "User lacks required role.");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Message.Should().Be("User lacks required role.");
    }

    [Fact]
    public void OnError_BusinessRuleViolationException_RemovesException()
    {
        var ex = new BusinessRuleViolationException("InsufficientSpaceRole", "User lacks required role.");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Exception.Should().BeNull();
    }

    // ── Conflict Exceptions ────────────────────────────────────

    [Fact]
    public void OnError_DuplicateSlugException_ReturnsConflictCode()
    {
        var ex = new DuplicateSlugException("test-slug");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public void OnError_DuplicateSlugException_RemovesException()
    {
        var ex = new DuplicateSlugException("test-slug");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Exception.Should().BeNull();
    }

    [Fact]
    public void OnError_DuplicateEmailException_ReturnsConflictCode()
    {
        var ex = new DuplicateEmailException(Guid.NewGuid(), "test@example.com");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public void OnError_DuplicateEmailException_RemovesException()
    {
        var ex = new DuplicateEmailException(Guid.NewGuid(), "test@example.com");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Exception.Should().BeNull();
    }

    [Fact]
    public void OnError_DuplicateSpaceNameException_ReturnsConflictCode()
    {
        var ex = new DuplicateSpaceNameException(Guid.NewGuid(), "marketing");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public void OnError_DuplicateSpaceNameException_RemovesException()
    {
        var ex = new DuplicateSpaceNameException(Guid.NewGuid(), "marketing");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Exception.Should().BeNull();
    }

    [Fact]
    public void OnError_DuplicateMembershipException_ReturnsConflictCode()
    {
        var ex = new DuplicateMembershipException(Guid.NewGuid(), Guid.NewGuid());
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public void OnError_DuplicateMembershipException_RemovesException()
    {
        var ex = new DuplicateMembershipException(Guid.NewGuid(), Guid.NewGuid());
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Exception.Should().BeNull();
    }

    // ── Unexpected Exceptions ──────────────────────────────────

    [Fact]
    public void OnError_UnexpectedException_ReturnsInternalErrorCode()
    {
        var ex = new InvalidOperationException("Internal state is corrupt");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Code.Should().Be("INTERNAL_ERROR");
    }

    [Fact]
    public void OnError_UnexpectedException_SanitizesMessage()
    {
        var ex = new InvalidOperationException("Internal state is corrupt — userId=abc123");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Message.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public void OnError_UnexpectedException_RemovesException()
    {
        var ex = new InvalidOperationException("Object reference not set");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Exception.Should().BeNull();
    }

    [Fact]
    public void OnError_UnexpectedException_InDevelopment_IncludesDetailExtension()
    {
        _env.EnvironmentName.Returns(Environments.Development);
        var ex = new InvalidOperationException("Sensitive detail here");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Extensions.Should().ContainKey("detail")
            .WhoseValue.Should().Be("Sensitive detail here");
        result.Extensions.Should().ContainKey("exceptionType")
            .WhoseValue.Should().Be("System.InvalidOperationException");
    }

    [Fact]
    public void OnError_UnexpectedException_InProduction_ExcludesDetailExtension()
    {
        var ex = new InvalidOperationException("Sensitive detail here");
        var error = CreateErrorWithException(ex);

        var result = CreateFilter().OnError(error);

        result.Extensions?.ContainsKey("detail").Should().NotBe(true);
        result.Extensions?.ContainsKey("exceptionType").Should().NotBe(true);
    }

    // ── Null Exception (HC-generated errors) ───────────────────

    [Fact]
    public void OnError_NullException_PassesThroughUnchanged()
    {
        var error = new Error("Schema validation failed", code: "HC_SCHEMA_ERROR");

        var result = CreateFilter().OnError(error);

        result.Should().BeSameAs(error);
    }
}
