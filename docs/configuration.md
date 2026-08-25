# Configuration

## Environment variables

### API Host (`backend/src/Api.Host`)

| Setting | Required | Description | Example |
|---|---:|---|---|
| `AZURE_FOUNDRY_PROJECT_ENDPOINT` | Yes | Foundry project endpoint | `https://<account>.services.ai.azure.com/api/projects/<project>` |
| `AzureSearch__Endpoint` | Yes | Azure AI Search service URL | `https://<search>.search.windows.net` |
| `AzureFoundryModels__EmbedEndpoint` | Yes | Embed deployment URL | `https://<account>.services.ai.azure.com/openai/deployments/cohere-embed-v4` |
| `AzureFoundryModels__RerankEndpoint` | Yes | Foundry account base URL for rerank | `https://<account>.services.ai.azure.com` |
| `Dataset__RootPath` | Yes (local) | Path to dataset seed | `../../../dataset-seed` |

### MCP Host (`backend/src/LoanWorkflow.Mcp`)

| Setting | Required | Description | Example |
|---|---:|---|---|
| `AzureSearch__Endpoint` | Yes | Search service URL | Same as API |
| `AzureFoundryModels__*` | Yes | Embed and rerank settings | See `.env.local.example` |
| `Dataset__RootPath` | Yes | Dataset seed root | `/app/dataset-seed` (container) |
| `DataSource__Mode` | Yes | `Local` or `Fabric` | `Local` by default |
| `DataSource__FabricLakehouse__WorkspaceName` | When Fabric | Fabric workspace display name | Required if `DataSource__Mode=Fabric` |
| `DataSource__FabricLakehouse__LakehouseName` | When Fabric | Fabric lakehouse name | Required if `DataSource__Mode=Fabric` |
| `DataSource__FabricLakehouse__TimeoutSeconds` | No | OneLake I/O timeout | `30` |
| `AZURE_CLIENT_ID` | When UAMI | User-assigned managed identity client ID | MCP container identity |
| `MCP_BASE_URL` | Yes (provisioning) | Public MCP URL for agent provisioning | `https://<mcp>.azurecontainerapps.io` |

### Agent provisioning

| Setting | Required | Description | Example |
|---|---:|---|---|
| `AZURE_FOUNDRY_PROJECT_ENDPOINT` | Yes | Foundry project endpoint | Same as API |
| `AZURE_AI_MODEL_DEPLOYMENT_NAME` | Yes | Agent model deployment | `cohere-command-a-plus` |
| `MCP_BASE_URL` | Yes | MCP public base URL | Deployed `mcpUrl` output |

## Agent configuration

Agent definitions live under `agent-provisioning/src/CohereLoanAndMortgage.AgentProvisioning/agents/`. Each agent specifies:

- `agent.json` — name, schema, governance file references
- `instructions.md` — system instructions
- `mcp.json` — MCP server path
- `governance.yaml` / `rogue.yaml` — tool sandbox and rogue detection

## Backend configuration

[`backend/src/Api.Host/appsettings.json`](../backend/src/Api.Host/appsettings.json):

| Section | Purpose |
|---|---|
| `AzureFoundry` | Agent names and project endpoint |
| `CaseWorkflow` | Pre-index case documents on workflow start |

## Frontend configuration

[`frontend/src/WebApp/appsettings.json`](../frontend/src/WebApp/appsettings.json):

| Setting | Required | Description | Example |
|---|---:|---|---|
| `ApiBaseUrl` | Yes | Backend base URL | `http://localhost:5038/` |
| `DatasetSeed:RootPath` | Yes (local) | Dataset seed for UI catalog | `../../../dataset-seed` |
| `WorkflowPolling:IntervalSeconds` | No | Poll interval | `2` |

For local development, copy [`backend/src/Api.Host/.env.local.example`](../backend/src/Api.Host/.env.local.example) to `.env.local`.
