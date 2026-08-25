# Backend

## Purpose

Hosts the loan and mortgage agentic workflow, REST API for the frontend, and the MCP tool server used by Foundry prompt agents.

## Architecture

```text
frontend (Blazor)
    -> Api.Host (Agent Framework workflow + REST API)
        -> Foundry prompt agents (document, underwriting, RAI, loan setup)
            -> LoanWorkflow.Mcp (HTTPS MCP endpoints)
                -> Azure AI Search + Cohere embed/rerank
                -> dataset-seed (local) or Fabric lakehouse (optional)
```

Projects:

| Project | Role |
|---|---|
| `Api.Host` | Workflow orchestration and REST API |
| `LoanWorkflow.Mcp` | MCP servers and retrieval tooling |
| `LoanWorkflow.Governance` | MCP tool governance and audit |

## Main workflow

The **basic workflow** runs:

1. **Document processing** — ingest case documents, index evidence, validate completeness.
2. **Underwriting** — evaluate application profile, policies, and evidence.
3. **Human approval** — reviewer approves or denies the underwriting outcome.
4. **Responsible AI** — review the human decision against policy and fairness signals.
5. **Loan setup** — produce account setup readiness output.

Factory: `LoanMortgageBasicWorkflowFactory`.

## Agent Framework orchestration

- Uses `Microsoft.Agents.AI.Workflows` with Foundry-hosted prompt agents.
- Bridges pass structured JSON between stages.
- Human approval uses a workflow request port; resume via API after reviewer action.

## MCP servers

See [`src/LoanWorkflow.Mcp/README.md`](src/LoanWorkflow.Mcp/README.md).

| MCP endpoint | Tools |
|---|---|
| `/document-retrieval/mcp` | `get_case_documents`, `enrich_customer_context`, `index_case_documents`, `search_case_evidence` |
| `/underwriting-rules/mcp` | `get_application_profile`, `search_case_evidence`, `get_underwriting_context`, `get_relevant_policies` |
| `/policy-knowledge/mcp` | `get_relevant_policies`, `get_policies_by_refs`, `validate_human_decision` |
| `/loan-setup/mcp` | `build_account_setup_draft` |

Default local URL: `http://localhost:5040`

## REST API

Base path: `/api/loan-mortgage`

## API endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/api/loan-mortgage/scenarios` | Lists available demo cases from dataset seed |
| GET | `/api/loan-mortgage/applications/{caseId}/documents` | Lists ingest documents for a case |
| GET | `/api/loan-mortgage/applications/{caseId}/documents/content` | Downloads a case document (`sourcePath` query) |
| POST | `/api/loan-mortgage/applications/{caseId}/workflow/basic/start` | Starts the basic agentic workflow |
| GET | `/api/loan-mortgage/executions/{executionId}/basic/status` | Gets workflow status and agent outputs |
| POST | `/api/loan-mortgage/applications/{caseId}/workflow/basic/executions/{executionId}/resume` | Resumes workflow after human approval |
| GET | `/health` | Health check |

## Configuration

Copy [`.env.local.example`](src/Api.Host/.env.local.example) to `.env.local` in `Api.Host`.

Key settings:

- `AZURE_FOUNDRY_PROJECT_ENDPOINT`
- `AzureSearch__Endpoint`
- `AzureFoundryModels__EmbedEndpoint` / `RerankEndpoint`
- `Dataset__RootPath`

See [`docs/configuration.md`](../docs/configuration.md).

## Local run

**Option A — VS Code compound (recommended)**

1. Configure `backend/src/Api.Host/.env.local`.
2. Run the **Full stack (API + MCP + Web)** launch compound in `.vscode/launch.json`.

**Option B — separate terminals**

```powershell
# Terminal 1 — MCP
cd backend/src/LoanWorkflow.Mcp
dotnet run

# Terminal 2 — API
cd backend/src/Api.Host
dotnet run

# Terminal 3 — seed policies (first run only)
cd backend/src/LoanWorkflow.Mcp
dotnet run -- --seed-policies
```

Default URLs: MCP `http://localhost:5040`, API `http://localhost:5038`.

Provision Foundry agents before starting workflows. See [`../agent-provisioning/README.md`](../agent-provisioning/README.md).

## Observability

- Application Insights when deployed via Bicep.
- MCP governance audit log at `data/agent-governance-audit/agent-governance-audit.jsonl` when governance is enabled.
- Workflow status returned by the status API includes per-agent JSON outputs.

## Troubleshooting

| Symptom | Possible cause | Resolution |
|---|---|---|
| `503` on start workflow | Foundry endpoint or agents missing | Verify provisioning and `AZURE_FOUNDRY_PROJECT_ENDPOINT` |
| Empty retrieval results | Search indexes not seeded | Run policy seed job or `dotnet run -- --seed-policies` locally |
| MCP tool errors | MCP not running or wrong URL in agents | Start MCP and re-provision agents with correct `MCP_BASE_URL` |
| Case not found | Missing `dataset-seed/cases/{caseId}/ingest/` | Pick a valid case (`case-01` … `case-20`) |

See [`docs/troubleshooting.md`](../docs/troubleshooting.md).
