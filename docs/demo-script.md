# Demo script

## Demo goal
Show an end-to-end multi-agent mortgage workflow with retrieval, MCP tools, Cohere models, human oversight, and responsible AI review — using only synthetic data.

## Setup
1. Deploy via **Deploy to Azure** or use a pre-deployed environment.
2. Open `frontendUrl` from deployment outputs.
3. Confirm the post-deployment ACI runner reached `Succeeded`; it runs the policy seed and agent provisioning jobs sequentially.
4. Have three cases ready: `case-01` (approve), `case-13` (deny), `case-17` (manual review).

## Step-by-step flow
1. **Open the frontend** — show the case picker and highlight synthetic data disclaimer.
2. **Select `case-01`** — walk through applicant summary and ingest documents.
3. **Start workflow** — explain document processing indexing evidence into Search with Cohere embeddings.
4. **Underwriting stage** — show policy retrieval, risk signals, and structured recommendation.
5. **Human approval** — approve or deny; explain this is a demonstration of HITL, not a production approval system.
6. **Responsible AI** — show how the agent reviews the human decision against policy.
7. **Loan setup** — show consolidated readiness output.
8. **Optional contrast** — repeat with `case-13` (deny) or `case-17` (borderline) if time allows.

## Expected outputs
| Case | Underwriting direction | Human action | RAI note |
|---|---|---|---|
| `case-01` | Approve | Approve | Supports approval |
| `case-13` | Deny | Approve (override) | Flags potential bias risk |
| `case-17` | Manual review | Either | Caveats depending on rationale |

Agent outputs include `summary`, `decision`, and `evidence` fields (plus role-specific fields for underwriting and RAI).

## Resetting the demo
- Start a new case from the picker (each run gets a new `executionId`).
- Redeploy or restart the API to clear in-memory execution state.
- Delete the resource group when finished to stop Azure charges.

See also [troubleshooting.md](./troubleshooting.md).
