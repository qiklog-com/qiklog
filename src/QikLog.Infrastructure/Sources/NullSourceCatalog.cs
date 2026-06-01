using QikLog.Core.Management;

namespace QikLog.Infrastructure.Sources;

public sealed class NullSourceCatalog : ISourceCatalog
{
    public Task<IReadOnlyList<SourceSummary>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SourceSummary>>([]);
}
