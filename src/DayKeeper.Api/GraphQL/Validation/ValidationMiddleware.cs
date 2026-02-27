using DayKeeper.Application.Exceptions;
using FluentValidation;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Logging;

namespace DayKeeper.Api.GraphQL.Validation;

/// <summary>
/// Hot Chocolate field middleware that automatically runs FluentValidation
/// on mutation fields before the resolver executes. Uses <see cref="InputFactory"/>
/// to construct the appropriate command record from the HC-generated input object,
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
        var argument = context.Selection.Field.Arguments.FirstOrDefault(
            a => string.Equals(a.Name, "input", StringComparison.Ordinal));

        if (argument is not null)
        {
            try
            {
                var hcInput = context.ArgumentValue<object?>("input");
                if (hcInput is not null)
                {
                    var command = InputFactory.TryCreate(fieldName, hcInput);
                    if (command is not null)
                    {
                        await ValidateAsync(command, context).ConfigureAwait(false);
                    }
                }
            }
            catch (InputValidationException)
            {
                throw; // Let validation errors propagate to HC error handling
            }
            catch (Exception ex)
            {
                // Safety net: log but don't block the resolver if factory/reflection fails
                var logger = context.Services.GetService<ILoggerFactory>()
                    ?.CreateLogger<ValidationMiddleware>();
                if (logger is not null)
                {
                    LogValidationMiddlewareFailed(logger, fieldName, ex);
                }
            }
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation middleware failed for field '{FieldName}'")]
    private static partial void LogValidationMiddlewareFailed(ILogger logger, string fieldName, Exception ex);
}
