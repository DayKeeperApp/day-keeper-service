using DayKeeper.Application.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DayKeeper.Api.GraphQL;

/// <summary>
/// Hot Chocolate error filter that maps domain exceptions to structured GraphQL
/// error codes and sanitizes unexpected exceptions to prevent leaking internal details.
/// Complements the <c>[Error&lt;T&gt;]</c> mutation convention error handling by
/// catching any exceptions that flow through HC's normal error pipeline.
/// </summary>
public sealed partial class DomainErrorFilter : IErrorFilter
{
    private readonly ILogger<DomainErrorFilter> _logger;
    private readonly IHostEnvironment _env;

    public DomainErrorFilter(ILogger<DomainErrorFilter> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            null => error,
            EntityNotFoundException ex => MapEntityNotFound(error, ex),
            InputValidationException ex => MapInputValidation(error, ex),
            BusinessRuleViolationException ex => MapBusinessRule(error, ex),
            DuplicateSlugException ex => MapConflict(error, ex.Message),
            DuplicateEmailException ex => MapConflict(error, ex.Message),
            DuplicateSpaceNameException ex => MapConflict(error, ex.Message),
            DuplicateMembershipException ex => MapConflict(error, ex.Message),
            _ => MapUnexpected(error),
        };
    }

    private static IError MapEntityNotFound(IError error, EntityNotFoundException ex)
    {
        var result = error
            .WithMessage(ex.Message)
            .WithCode("NOT_FOUND")
            .WithException(null)
            .SetExtension("entityName", ex.EntityName);

        if (ex.EntityId != Guid.Empty)
        {
            result = result.SetExtension("entityId", ex.EntityId);
        }

        return result;
    }

    private static IError MapInputValidation(IError error, InputValidationException ex)
    {
        var result = error
            .WithMessage(ex.Message)
            .WithCode("VALIDATION_ERROR")
            .WithException(null);

        foreach (var (field, messages) in ex.Errors)
        {
            result = result.SetExtension($"validation.{field}", messages);
        }

        return result;
    }

    private static IError MapBusinessRule(IError error, BusinessRuleViolationException ex)
    {
        return error
            .WithMessage(ex.Message)
            .WithCode("BUSINESS_RULE_VIOLATION")
            .WithException(null)
            .SetExtension("rule", ex.Rule);
    }

    private static IError MapConflict(IError error, string message)
    {
        return error
            .WithMessage(message)
            .WithCode("CONFLICT")
            .WithException(null);
    }

    private IError MapUnexpected(IError error)
    {
        LogUnexpectedException(_logger, error.Exception!);

        var result = error
            .WithMessage("An unexpected error occurred.")
            .WithCode("INTERNAL_ERROR")
            .WithException(null);

        if (_env.IsDevelopment())
        {
            result = result
                .SetExtension("exceptionType", error.Exception!.GetType().FullName)
                .SetExtension("detail", error.Exception.Message);
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "An unhandled exception reached the GraphQL error filter.")]
    private static partial void LogUnexpectedException(ILogger logger, Exception ex);
}
