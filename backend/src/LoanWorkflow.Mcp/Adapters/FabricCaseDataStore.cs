using Azure;
using LoanWorkflow.Mcp.Options;
using Microsoft.Extensions.Options;

namespace LoanWorkflow.Mcp.Adapters;

public sealed class FabricCaseDataStore : ICaseDataStore
{
    private readonly IFabricLakehouseClient _client;
    private readonly string _evidenceRoot;

    public FabricCaseDataStore(IFabricLakehouseClient client, IOptions<DataSourceOptions> options)
    {
        _client = client;
        _evidenceRoot = string.IsNullOrWhiteSpace(options.Value.FabricLakehouse?.EvidenceRoot)
            ? "Files/bronze"
            : options.Value.FabricLakehouse!.EvidenceRoot.TrimEnd('/');
    }

    public async Task<string> ReadDocumentAsync(string caseId, EvidenceCategory category, string fileName, CancellationToken cancellationToken = default)
    {
        ValidateCaseId(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var path = FilePath(caseId, category, fileName);
        try
        {
            return await _client.ReadFileAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException ex)
        {
            throw new FileNotFoundException($"Case document not found: {path}", ex);
        }
    }

    public async Task<IReadOnlyList<string>> ListDocumentsAsync(string caseId, EvidenceCategory category, CancellationToken cancellationToken = default)
    {
        ValidateCaseId(caseId);
        var categoryPath = CategoryPath(caseId, category);

        try
        {
            var files = await _client.ListFilesAsync(categoryPath, cancellationToken).ConfigureAwait(false);
            return files
                .Where(name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                               && !name.StartsWith("SCHEMA", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new KeyNotFoundException($"Case category directory not found: {categoryPath}", ex);
        }
    }

    private string CategoryPath(string caseId, EvidenceCategory category) =>
        $"{_evidenceRoot}/cases/{caseId.Trim()}/fabric-pre-requisite-data/{EvidenceCategoryFolders.For(category)}";

    private string FilePath(string caseId, EvidenceCategory category, string fileName) =>
        $"{CategoryPath(caseId, category)}/{fileName.Trim().TrimStart('/')}";

    private static void ValidateCaseId(string caseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
    }
}
