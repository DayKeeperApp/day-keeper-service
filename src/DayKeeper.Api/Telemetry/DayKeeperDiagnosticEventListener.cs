using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;

namespace DayKeeper.Api.Telemetry;

public sealed class DayKeeperDiagnosticEventListener : ExecutionDiagnosticEventListener
{
    private readonly DayKeeperMetrics _metrics;

    public DayKeeperDiagnosticEventListener(DayKeeperMetrics metrics)
    {
        _metrics = metrics;
    }

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        return new RequestScope(_metrics, context);
    }

    private sealed class RequestScope : IDisposable
    {
        private readonly DayKeeperMetrics _metrics;
        private readonly IRequestContext _context;

        public RequestScope(DayKeeperMetrics metrics, IRequestContext context)
        {
            _metrics = metrics;
            _context = context;
        }

        public void Dispose()
        {
            var operation = _context.Operation;
            if (operation is null)
            {
                return;
            }

            var operationType = operation.Type.ToString();
            var operationName = operation.Name ?? "anonymous";
            _metrics.RecordGraphQLOperation(operationType, operationName);

            if (string.Equals(operationType, "Mutation", StringComparison.Ordinal))
            {
                TryRecordEntityMutation(operationName);
            }
        }

        private void TryRecordEntityMutation(string operationName)
        {
            var (crudOp, entityType) = ParseMutationName(operationName);
            if (crudOp is not null && entityType is not null)
            {
                _metrics.RecordEntityMutation(entityType, crudOp);
            }
        }

        private static (string? CrudOp, string? EntityType) ParseMutationName(string name)
        {
            ReadOnlySpan<char> span = name;

            if (span.StartsWith("Create", StringComparison.Ordinal))
            {
                return ("Create", name[6..]);
            }

            if (span.StartsWith("Update", StringComparison.Ordinal))
            {
                return ("Update", name[6..]);
            }

            if (span.StartsWith("Delete", StringComparison.Ordinal))
            {
                return ("Delete", name[6..]);
            }

            if (span.StartsWith("Complete", StringComparison.Ordinal))
            {
                return ("Update", name[8..]);
            }

            if (span.StartsWith("Add", StringComparison.Ordinal))
            {
                return ("Create", name[3..]);
            }

            if (span.StartsWith("Remove", StringComparison.Ordinal))
            {
                return ("Delete", name[6..]);
            }

            return (null, null);
        }
    }
}
