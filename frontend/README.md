# Frontend

## Purpose

Blazor Server web app for selecting demo cases, starting the loan workflow, reviewing agent outputs, and submitting human-in-the-loop decisions.

## User experience

1. Pick a case from the seeded catalog (`case-01` … `case-20`).
2. Review applicant summary and ingest documents.
3. Start the basic workflow and watch stage progress.
4. Inspect document processing, underwriting, responsible AI, and loan setup panels.
5. Approve or deny when the workflow pauses for human review.
6. Review retrieval traces and governance summaries where shown.

Highlight cases: `case-01` (approve), `case-13` / `case-15` (deny), `case-17` / `case-18` (manual review).

## Configuration

[`appsettings.json`](src/WebApp/appsettings.json):

| Setting | Default | Description |
|---|---|---|
| `ApiBaseUrl` | `http://localhost:5038/` | Backend API base URL |
| `DatasetSeed:RootPath` | `../../../dataset-seed` | Path to dataset seed for UI catalog |
| `WorkflowPolling:IntervalSeconds` | `2` | Status poll interval |
| `WorkflowPolling:MaxDurationMinutes` | `10` | Max poll duration |

## Local run

```powershell
cd frontend/src/WebApp
dotnet run
```

Default URL: `http://localhost:5147`

Ensure the API is running at the configured `ApiBaseUrl`.

Or use the **Full stack (API + MCP + Web)** compound in `.vscode/launch.json`.

## Build

```powershell
dotnet restore frontend/src/WebApp/Cohere.LoanProcessing.WebApp.csproj --configfile NuGet.config
dotnet build frontend/src/WebApp/Cohere.LoanProcessing.WebApp.csproj --no-restore
```

Publish:

```powershell
dotnet restore frontend/src/WebApp/Cohere.LoanProcessing.WebApp.csproj --configfile NuGet.config
dotnet publish frontend/src/WebApp/Cohere.LoanProcessing.WebApp.csproj -c Release --no-restore
```

## Environment variables

| Variable | Description |
|---|---|
| `ASPNETCORE_URLS` | Bind URL (set in `launchSettings.json`) |
| `ApiBaseUrl` | Override backend URL via configuration or environment |

In Azure, the Container App sets `ApiBaseUrl` to the deployed API URL.

## Connecting to backend

The frontend calls `api/loan-mortgage/*` on the configured API host through `LoanApiClient`.

Health check: `GET /health` on the frontend host.

## Troubleshooting

| Symptom | Possible cause | Resolution |
|---|---|---|
| Cases list empty | Dataset seed path wrong | Check `DatasetSeed:RootPath` relative to WebApp cwd |
| Workflow stuck polling | API or agents not running | Verify API health at `/health` and agent provisioning |
| Connection refused | `ApiBaseUrl` mismatch | Match API port (`5038` locally) |
| Human review not shown | Workflow not at approval stage | Wait for underwriting to complete; check status API |
