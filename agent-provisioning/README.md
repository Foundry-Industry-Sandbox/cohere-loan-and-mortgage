# Agent provisioning

## Purpose

Creates or updates the four Microsoft Foundry prompt agents used by the loan and mortgage workflow API.

In the standard Azure deployment flow, provisioning runs automatically as a Container Apps Job after infrastructure and MCP wiring complete. You do not need to run this CLI manually after clicking **Deploy to Azure**.

## Prompt agents included

| Agent | Model | Tools | Memory | Purpose |
|---|---|---|---|---|
| `document-processing-agent` | Cohere Command A+ | `/document-retrieval/mcp` | Not configured | Extract and validate document evidence |
| `underwriting-agent` | Cohere Command A+ | `/underwriting-rules/mcp` | Not configured | Evaluate risk and produce underwriting recommendation |
| `responsible-ai-agent` | Cohere Command A+ | `/policy-knowledge/mcp` | Not configured | Review fairness, policy, and governance concerns |
| `loan-setup-agent` | Cohere Command A+ | `/loan-setup/mcp` | Not configured | Consolidate prior outputs and produce setup readiness |

## Required environment variables

| Variable | Required | Description |
|---|---:|---|
| `AZURE_FOUNDRY_PROJECT_ENDPOINT` | Yes | Foundry project endpoint |
| `AZURE_AI_MODEL_DEPLOYMENT_NAME` | Yes | Model deployment name (default in infra: `cohere-command-a-plus`) |
| `MCP_BASE_URL` | Yes | Public HTTPS base URL of the MCP Container App |

## Provisioning flow

During **Deploy to Azure**, [`infra/main.bicep`](../infra/main.bicep):

1. Provisions Foundry, Search, and model deployments.
2. Deploys the MCP Container App.
3. Starts the policy seed job.
4. Starts the agent provisioning Container Apps Job.
5. Waits for the job to finish before completing the deployment.

The provisioning container image is built from [`Dockerfile`](Dockerfile).

## How to create agents

For local or maintenance runs after MCP is reachable:

```powershell
$env:AZURE_FOUNDRY_PROJECT_ENDPOINT = "https://<project>.services.ai.azure.com/api/projects/<project>"
$env:AZURE_AI_MODEL_DEPLOYMENT_NAME = "cohere-command-a-plus"
$env:MCP_BASE_URL = "https://<mcp-app>.azurecontainerapps.io"

dotnet run --project agent-provisioning/src/CohereLoanAndMortgage.AgentProvisioning
```

Or use the helper script:

```powershell
./agent-provisioning/scripts/provision-agents.ps1
```

Pass `--agents agent-provisioning/src/CohereLoanAndMortgage.AgentProvisioning/agents` if you need an explicit agents root.

## How to update agents

1. Edit agent assets under `src/CohereLoanAndMortgage.AgentProvisioning/agents/<agent-name>/`.
2. Re-run provisioning (Azure job on redeploy, or local command above).
3. The CLI compares a definition fingerprint and creates a new version only when assets changed.

Agent-as-code layout per agent:

```text
agents/<agent-name>/
  agent.json
  instructions.md
  mcp.json
  governance.yaml
  rogue.yaml
```

Shared schemas live in `src/CohereLoanAndMortgage.AgentProvisioning/shared/`.

## How to validate provisioning

1. Confirm the provisioning job completed successfully in Azure Portal or deployment logs.
2. Open the Foundry project and verify all four agents exist.
3. Start a workflow from the frontend and confirm each agent stage completes.

## Troubleshooting

| Symptom | Possible cause | Resolution |
|---|---|---|
| `McpBaseUrl is required` | MCP URL not set | Set `MCP_BASE_URL` to the public MCP host |
| Provisioning job fails | Model quota or deployment name mismatch | Check Foundry deployments and `modelDeploymentCapacity` in Bicep |
| Agent tools fail at runtime | MCP not reachable from Foundry | Verify MCP ingress URL and that agents point to the correct MCP paths |
| No new agent version created | Definition unchanged | Expected idempotent behavior; change agent assets to trigger update |

See also [`docs/agent-foundry-governance.md`](../docs/agent-foundry-governance.md) for governance policy layout.
