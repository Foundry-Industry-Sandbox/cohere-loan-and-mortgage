# Infrastructure

Bicep templates and scripts to deploy the sample into an Azure subscription. Data generation is not deployed.

## Resources deployed

| Resource | Purpose |
|---|---|
| Microsoft Foundry account and project | Prompt agents and model deployments |
| Cohere model deployments | Command A+ (agents), embed-v4, rerank-v4-pro |
| Azure AI Search | Evidence and policy retrieval indexes |
| Container Apps Environment | Hosts API (backend), MCP, and frontend |
| Container Apps Jobs | Policy seed and agent provisioning |
| Azure Container Instances | Runs post-deployment jobs with a user-assigned managed identity |
| User-assigned managed identities | API, MCP, and provisioning workloads |
| Log Analytics / Application Insights | Telemetry |
| Microsoft Fabric (optional) | Lakehouse for case data when enabled |

## Bicep modules

| Module | File |
|---|---|
| Naming | `modules/naming.bicep` |
| Data services | `modules/data-services.bicep` |
| Platform | `modules/platform.bicep` |
| Foundry | `modules/foundry.bicep` |
| Security / RBAC | `modules/security.bicep` |
| Container Apps | `modules/container-apps.bicep` |
| Container Jobs | `modules/container-jobs.bicep` |
| Fabric (optional) | `modules/fabric-provision.bicep` |
| Post-deploy runner and optional Fabric seed | `modules/post-deploy-scripts.bicep` |

Entry point: [`main.bicep`](main.bicep).

## Parameters

Key parameters in `main.bicep`:

| Parameter | Default | Description |
|---|---|---|
| `baseName` | `cohereloan` | Resource name prefix |
| `modelDeploymentName` | `cohere-command-a-plus` | Agent model deployment |
| `enableFabric` | `false` | Enable Fabric lakehouse integration |
| `apiContainerImage` | `ghcr.io/Foundry-Industry-Sandbox/cohereloan-api:demo` | API container image |
| `mcpContainerImage` | `ghcr.io/Foundry-Industry-Sandbox/cohereloan-mcp:demo` | MCP container image |
| `frontendContainerImage` | `ghcr.io/Foundry-Industry-Sandbox/cohereloan-web:demo` | Frontend container image |

See [`main.parameters.json`](main.parameters.json) and the Deploy to Azure UI in [`createUiDefinition.json`](createUiDefinition.json). Full parameter reference: [`docs/deployment-parameters.md`](../docs/deployment-parameters.md).

## Outputs

| Output | Use |
|---|---|
| `frontendUrl` | Open the demo UI |
| `apiUrl` | Backend REST API |
| `mcpUrl` | MCP base URL for agent tools |
| `foundryProjectEndpoint` | Foundry project endpoint |
| `searchServiceEndpoint` | Azure AI Search endpoint |
| `postDeployRunnerName` | ACI runner that starts and monitors the initialization jobs |

The ARM deployment completes after the post-deployment ACI runner starts. Before using the application, confirm that the container group's `instanceView.state` is `Succeeded`:

```bash
az container show --resource-group <resource-group-name> --name <postDeployRunnerName> --query instanceView.state --output tsv
```

## Validate and update the ARM template

The repository includes a Marketplace-compatible, standard ARM template at [`mainTemplate.json`](mainTemplate.json). It intentionally uses ARM language version 1.0 and includes the Partner Center attribution resource. Compile Bicep changes to a temporary file for validation:

```bash
az bicep build --file infra/main.bicep --outfile <temporary-path>/mainTemplate.json
```

Do not overwrite the checked-in template with direct Bicep output when the compiler emits ARM language version 2.0, which Azure Managed Applications does not support. Carry the validated resource changes into the standard ARM template while preserving the Partner Center attribution resource.

## Deploy to Azure button support

The root README **Deploy to Azure** button targets [`mainTemplate.json`](mainTemplate.json) with [`createUiDefinition.json`](createUiDefinition.json).

After changing Bicep, update `mainTemplate.json` and rebuild `partner-center/AzureApplicationPackage-Cohere-FSI.zip` from `mainTemplate.json`, `createUiDefinition.json`, and `viewDefinition.json` before the button or Marketplace package reflects your changes.

## Cleanup

```bash
az group delete --name <resource-group-name> --yes --no-wait
```

Additional scripts:

| Script | Purpose |
|---|---|
| `scripts/seed-fabric-data.ps1` | Fabric data seeding (when Fabric is enabled) |
| `scripts/setup-fabric-provision-identity.ps1` | Create Fabric UAMI and workspace role. See [docs/fabric-setup.md](../docs/fabric-setup.md). |
