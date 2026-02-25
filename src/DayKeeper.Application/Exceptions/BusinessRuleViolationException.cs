namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when an operation violates a business rule
/// (e.g., removing the last owner from a space).
/// </summary>
public sealed class BusinessRuleViolationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class.
    /// </summary>
    /// <param name="rule">A machine-readable identifier for the violated rule.</param>
    /// <param name="message">A human-readable description of the violation.</param>
    public BusinessRuleViolationException(string rule, string message)
        : base(message)
    {
        Rule = rule;
    }

    /// <summary>A machine-readable identifier for the violated rule.</summary>
    public string Rule { get; }
}
