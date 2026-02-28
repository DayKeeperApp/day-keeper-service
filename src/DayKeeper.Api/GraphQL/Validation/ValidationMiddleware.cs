using DayKeeper.Application.Exceptions;
using FluentValidation;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Logging;

namespace DayKeeper.Api.GraphQL.Validation;

/// <summary>
/// Hot Chocolate field middleware that automatically runs FluentValidation
/// on mutation fields before the resolver executes. Uses <see cref="InputFactory"/>
/// to construct the appropriate command record from the resolved arguments,
/// then validates it against the registered <see cref="IValidator{T}"/>.
/// </summary>
public sealed partial class ValidationMiddleware
{
    private readonly FieldDelegate _next;

    public ValidationMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        var fieldName = context.Selection.Field.Name;

        try
        {
            var command = InputFactory.TryCreate(fieldName, context);
            if (command is not null)
            {
                await ValidateAsync(command, context).ConfigureAwait(false);
            }
        }
        catch (InputValidationException)
        {
            throw; // Propagate to HC ErrorMiddleware
        }
        catch (Exception ex)
        {
            LogIfPossible(context, fieldName, ex);
        }

        await _next(context).ConfigureAwait(false);
    }

    private static async Task ValidateAsync(object command, IMiddlewareContext context)
    {
        var commandType = command.GetType();
        var validatorType = typeof(IValidator<>).MakeGenericType(commandType);
        var validator = context.Services.GetService(validatorType);

        if (validator is null)
            return;

        var contextType = typeof(ValidationContext<>).MakeGenericType(commandType);
        var validationContext = (IValidationContext)Activator.CreateInstance(contextType, command)!;

        var result = await ((IValidator)validator)
            .ValidateAsync(validationContext, context.RequestAborted)
            .ConfigureAwait(false);

        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName, StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray(),
                    StringComparer.Ordinal);

            throw new InputValidationException(errors);
        }
    }

    private static void LogIfPossible(IMiddlewareContext context, string fieldName, Exception ex)
    {
        var logger = context.Services.GetService<ILoggerFactory>()
            ?.CreateLogger<ValidationMiddleware>();
        if (logger is not null)
        {
            LogValidationMiddlewareFailed(logger, fieldName, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation middleware failed for field '{FieldName}'")]
    private static partial void LogValidationMiddlewareFailed(ILogger logger, string fieldName, Exception ex);
}
