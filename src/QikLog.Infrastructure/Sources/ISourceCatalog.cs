using QikLog.Core.Management;

namespace QikLog.Infrastructure.Sources;

public interface ISourceCatalog
{
    Task<IReadOnlyList<SourceSummary>> ListAsync(CancellationToken cancellationToken);
}
