namespace LoanWorkflow.Mcp.Adapters;

public interface IFabricLakehouseClient
{
    Task<string> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListFilesAsync(string relativeDirectory, CancellationToken cancellationToken = default);
}
