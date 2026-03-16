using System.Diagnostics.Metrics;

namespace DayKeeper.Api.Telemetry;

public sealed class DayKeeperMetrics
{
    public const string MeterName = "DayKeeper.Api";

    private readonly Counter<long> _graphqlOperations;
    private readonly Counter<long> _graphqlErrors;
    private readonly Counter<long> _entityMutations;
    private readonly Counter<long> _syncPulls;
    private readonly Histogram<int> _syncPullChanges;
    private readonly Counter<long> _syncPushes;
    private readonly Histogram<int> _syncPushChanges;
    private readonly Counter<long> _syncConflicts;
    private readonly Counter<long> _attachmentUploads;
    private readonly Histogram<long> _attachmentUploadBytes;

    public DayKeeperMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _graphqlOperations = meter.CreateCounter<long>("daykeeper.graphql.operations", description: "GraphQL operations executed");
        _graphqlErrors = meter.CreateCounter<long>("daykeeper.graphql.errors", description: "GraphQL errors by code");
        _entityMutations = meter.CreateCounter<long>("daykeeper.entity.mutations", description: "Entity CRUD operations");
        _syncPulls = meter.CreateCounter<long>("daykeeper.sync.pull.count", description: "Sync pull requests");
        _syncPullChanges = meter.CreateHistogram<int>("daykeeper.sync.pull.changes", description: "Changes returned per sync pull");
        _syncPushes = meter.CreateCounter<long>("daykeeper.sync.push.count", description: "Sync push requests");
        _syncPushChanges = meter.CreateHistogram<int>("daykeeper.sync.push.changes", description: "Changes per sync push");
        _syncConflicts = meter.CreateCounter<long>("daykeeper.sync.conflicts", description: "Sync LWW conflicts");
        _attachmentUploads = meter.CreateCounter<long>("daykeeper.attachments.uploads", description: "Attachment uploads");
        _attachmentUploadBytes = meter.CreateHistogram<long>("daykeeper.attachments.upload_bytes", unit: "By", description: "Attachment upload size");
    }

    public void RecordGraphQLOperation(string operationType, string operationName)
    {
        _graphqlOperations.Add(1,
            new KeyValuePair<string, object?>("operation_type", operationType),
            new KeyValuePair<string, object?>("operation_name", operationName));
    }

    public void RecordGraphQLError(string errorCode)
    {
        _graphqlErrors.Add(1, new KeyValuePair<string, object?>("error_code", errorCode));
    }

    public void RecordEntityMutation(string entityType, string operation)
    {
        _entityMutations.Add(1,
            new KeyValuePair<string, object?>("entity_type", entityType),
            new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordSyncPull(int changeCount)
    {
        _syncPulls.Add(1);
        _syncPullChanges.Record(changeCount);
    }

    public void RecordSyncPush(int changeCount, int conflictCount)
    {
        _syncPushes.Add(1);
        _syncPushChanges.Record(changeCount);
        if (conflictCount > 0)
        {
            _syncConflicts.Add(conflictCount);
        }
    }

    public void RecordAttachmentUpload(long fileSize)
    {
        _attachmentUploads.Add(1);
        _attachmentUploadBytes.Record(fileSize);
    }
}
