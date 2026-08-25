# Troubleshooting

## Deployment issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| Deploy to Azure fails early | Invalid region or quota | Try another region; check Foundry and model quota |
| Deployment succeeds but the app is not ready | Post-deployment runner is still running or failed | Check the ACI runner state and logs, then inspect the Container Apps Job logs |
| `frontendUrl` not reachable | Container App still starting | Wait and retry; check Container App revision status |

ARM completes after the post-deployment ACI runner starts, not after its two jobs finish. Use the `postDeployRunnerName` deployment output and confirm that the runner reaches `Succeeded`:

```bash
az container show --resource-group <resource-group-name> --name <postDeployRunnerName> --query instanceView.state --output tsv
az container logs --resource-group <resource-group-name> --name <postDeployRunnerName>
```

## Model availability issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| `no_capacity` errors | Low deployment capacity | Increase `modelDeploymentCapacity` in Bicep |
| Embed/rerank throttling | Capacity too low for indexing | Increase `embedDeploymentCapacity` or `rerankDeploymentCapacity` |
| Model not found | Name/version unavailable in region | Adjust `cohereModelName` / version parameters per Foundry catalog |

## Agent provisioning issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| Provisioning job failed | Missing `MCP_BASE_URL` or endpoint | Verify MCP app is running; check job environment variables |
| Agents exist but tools fail | Wrong MCP path in agent definition | Re-provision after MCP URL is stable |
| No agent version update | Idempotent fingerprint unchanged | Expected if definitions did not change |

## Backend startup issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| API fails on start | Missing Foundry or Search config | Fill `.env.local` from `.env.local.example` |
| `Scenario catalog unavailable` | Dataset path wrong | Verify `Dataset__RootPath` points to `dataset-seed` |
| Workflow `503` | Agents not provisioned | Complete provisioning job or run local provisioner |
| Empty search results | Indexes not created | Run policy seed (`dotnet run -- --seed-policies` locally) |

## Frontend connection issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| API connection refused | Wrong `ApiBaseUrl` | Match API port (`5038` local) |
| Cases not listed | Bad `DatasetSeed:RootPath` | Path is relative to WebApp working directory |
| Polling timeout | Workflow exceeded `MaxDurationMinutes` | Check API logs; increase polling settings for slow runs |

## Dataset issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| Case not found | Invalid `caseId` | Use `case-01` … `case-20` with `ingest/loan_application.txt` |
| Missing documents | Incomplete case folder | Regenerate from `data-generation` or pick another case |
| Stale case metadata | `catalog.json` out of date | Re-run `build_case_folders.py` in data-generation |

## Quota and regional availability

| Symptom | Possible cause | Resolution |
|---|---|---|
| Search SKU limits | Basic tier document limits | Acceptable for demo; scale SKU for larger tests |
| Container Apps limits | Subscription quotas | Request quota increase or use another subscription |
| Foundry project limits | Account capacity | Use supported region; reduce parallel workflow runs |

For local development, use the **Full stack** launch compound in `.vscode/launch.json` and verify each service `/health` endpoint.
