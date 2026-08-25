# Deployment parameters

Parameters for the custom Deploy to Azure template ([`infra/mainTemplate.json`](../infra/mainTemplate.json)), defined in [`infra/main.bicep`](../infra/main.bicep). The portal UI is driven by [`infra/createUiDefinition.json`](../infra/createUiDefinition.json).

## Template parameters

| Parameter | Default | Description |
|---|---|---|
| `baseName` | `cohereloan` | Short prefix used for deployed resource names. |
| `location` | Resource group location | Azure region for all deployed resources. |
| `modelDeploymentName` | `cohere-command-a-plus` | Foundry model deployment name used by all prompt agents. |
| `modelDeploymentSkuName` | `GlobalStandard` | SKU for the agent model deployment. Use `GlobalStandard` for serverless; use a provisioned SKU only if available for the model and region. |
| `modelDeploymentCapacity` | `100` | Capacity units for the agent model deployment. Increase when agents fail with `no_capacity` during peak load. |
| `cohereModelName` | `Cohere-command-a-plus-05-2026` | Cohere Command A model name in the Foundry catalog. |
| `cohereModelVersion` | `1` | Cohere Command A model version. |
| `embedDeploymentName` | `cohere-embed-v4` | Foundry deployment name for Cohere embed-v4. |
| `embedModelName` | `embed-v-4-0` | Cohere embed model name in the Foundry catalog. |
| `embedModelVersion` | `1` | Cohere embed model version. |
| `embedDeploymentCapacity` | `1000` | Capacity units for the embed deployment. Increase for faster case evidence indexing and fewer throttling failures. |
| `rerankDeploymentName` | `cohere-rerank-v4-pro` | Foundry deployment name for Cohere rerank v4 pro. |
| `rerankModelName` | `Cohere-rerank-v4.0-pro` | Cohere rerank model name in the Foundry catalog. |
| `rerankModelVersion` | `1` | Cohere rerank model version. |
| `rerankDeploymentCapacity` | `5` | Capacity units for the rerank deployment. Increase for faster retrieval reranking and fewer throttling failures. |
| `searchSku` | `basic` | Azure AI Search SKU for demo retrieval indexes. |
| `apiContainerImage` | `ghcr.io/Foundry-Industry-Sandbox/cohereloan-api:demo` | Full container image URI for the API host Container App. |
| `mcpContainerImage` | `ghcr.io/Foundry-Industry-Sandbox/cohereloan-mcp:demo` | Full container image URI for the MCP host Container App. |
| `frontendContainerImage` | `ghcr.io/Foundry-Industry-Sandbox/cohereloan-web:demo` | Full container image URI for the frontend Container App. |
| `provisioningContainerImage` | `ghcr.io/Foundry-Industry-Sandbox/cohereloan-provisioning:demo` | Full container image URI for the agent provisioning Container Apps Job. |
| `enableFabric` | `false` | When `true`, provisions a Fabric lakehouse, seeds lakehouse data, and configures MCP to read case context from Fabric. Requires `fabricWorkspaceName`. |
| `fabricWorkspaceName` | `''` | Fabric workspace name. Required when `enableFabric` is `true`. Must be capacity-backed and accessible to the operator. |
| `fabricLakehouseName` | `LoanProcessingLakehouse` | Fabric lakehouse name. Created at deploy time if missing when Fabric is enabled. |
| `fabricIdentityName` | `fabric-identity` | Optional. Name of an existing user-assigned managed identity in the **same resource group** as the deployment. Defaults to `fabric-identity`. Create with `infra/scripts/setup-fabric-provision-identity.ps1`. Used as the MCP identity and Fabric seed script identity. |
| `fabricRepositoryArchiveUrl` | `https://github.com/Foundry-Industry-Sandbox/cohere-loan-and-mortgage/archive/refs/heads/main.zip` | Repository archive URL the Fabric seed script downloads to fetch `infra/scripts/` and `dataset-seed/`. |

## Deployment outputs

After a successful deployment, use these outputs:

| Output | Use |
|---|---|
| `frontendUrl` | Open the demo UI |
| `apiUrl` | Backend REST API |
| `mcpUrl` | MCP base URL (used during agent provisioning) |
| `foundryProjectEndpoint` | Microsoft Foundry project endpoint |
| `searchServiceEndpoint` | Azure AI Search endpoint |
| `postDeployRunnerName` | Azure Container Instances runner for policy seeding and agent provisioning. Wait for its `instanceView.state` to reach `Succeeded`. |

See also [`infra/README.md`](../infra/README.md) and [architecture.md](./architecture.md).
