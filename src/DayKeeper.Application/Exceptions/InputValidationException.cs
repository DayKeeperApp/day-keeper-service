namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when FluentValidation detects one or more input constraint violations.
/// </summary>
public sealed class InputValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InputValidationException"/> class.
    /// </summary>
    /// <param name="errors">Field-level validation errors keyed by property name.</param>
    public InputValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    /// <summary>
    /// Validation errors keyed by property name, each with one or more error messages.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }
}
