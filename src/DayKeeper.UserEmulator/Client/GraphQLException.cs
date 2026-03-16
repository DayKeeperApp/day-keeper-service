namespace DayKeeper.UserEmulator.Client;

public sealed class GraphQLException : Exception
{
    public GraphQLException(string message) : base(message)
    {
    }
}
