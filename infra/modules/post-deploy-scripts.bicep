param location string
param resourceTags object
param deploymentSuffix string
param deploymentScriptIdentityName string
param foundryAccountName string
param foundryProjectName string
param policySeedJobName string
param provisioningJobName string
param enableFabric bool
param fabricIdentityName string = 'fabric-identity'
param fabricWorkspaceId string
param fabricWorkspaceName string
param fabricLakehouseId string
param fabricLakehouseName string
param fabricRepositoryArchiveUrl string

resource fabricUami 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = if (enableFabric) {
  name: fabricIdentityName
}

resource foundryAccount 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: foundryAccountName
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' existing = {
  parent: foundryAccount
  name: foundryProjectName
}

resource deploymentScriptIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: deploymentScriptIdentityName
  location: location
  tags: resourceTags
}

resource deploymentScriptContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, deploymentScriptIdentity.id, 'Contributor')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
    principalId: deploymentScriptIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource deploymentScriptFoundryContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryAccount.id, deploymentScriptIdentity.id, 'CognitiveServicesContributor')
  scope: foundryAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '25fbc0a9-bd7c-42a3-aa1a-3b75d497ee68')
    principalId: deploymentScriptIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
  dependsOn: [
    foundryProject
  ]
}

resource deploymentScriptFoundryUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryAccount.id, deploymentScriptIdentity.id, 'FoundryUser')
  scope: foundryAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d')
    principalId: deploymentScriptIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
  dependsOn: [
    foundryProject
  ]
}

resource postDeployRunner 'Microsoft.ContainerInstance/containerGroups@2023-05-01' = {
  name: 'run-post-deploy-${deploymentSuffix}'
  location: location
  tags: resourceTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${deploymentScriptIdentity.id}': {}
    }
  }
  properties: {
    containers: [
      {
        name: 'post-deploy-runner'
        properties: {
          image: 'mcr.microsoft.com/azure-cli:2.62.0'
          command: [
            '/bin/bash'
            '-c'
            '''
              set -euo pipefail

              echo "Waiting for role assignments and Foundry deployments to settle..."
              sleep 180

              authenticated=false
              for attempt in $(seq 1 12); do
                echo "Signing in with the post-deployment managed identity (attempt ${attempt}/12)..."
                if az login --identity --username "${AZURE_CLIENT_ID}" --allow-no-subscriptions >/dev/null && \
                   az account set --subscription "${AZURE_SUBSCRIPTION_ID}"; then
                  authenticated=true
                  break
                fi
                sleep 15
              done

              if [ "${authenticated}" != "true" ]; then
                echo "Managed identity sign-in failed after 12 attempts." >&2
                exit 1
              fi

              az extension add --name containerapp --upgrade

              run_job() {
                local job_name="$1"
                local container_name="$2"
                local display_name="$3"
                local execution
                local status

                echo "Starting ${display_name} job..."
                execution=$(az containerapp job start \
                  --name "${job_name}" \
                  --resource-group "${RESOURCE_GROUP}" \
                  --query name -o tsv)

                if [ -z "${execution}" ]; then
                  echo "${display_name} job did not return an execution name." >&2
                  return 1
                fi

                echo "${display_name} job execution: ${execution}"

                for i in $(seq 1 120); do
                  if ! status=$(az containerapp job execution show \
                    --name "${job_name}" \
                    --resource-group "${RESOURCE_GROUP}" \
                    --job-execution-name "${execution}" \
                    --query properties.status -o tsv); then
                    echo "Unable to read ${display_name} job status; retrying..."
                    sleep 15
                    continue
                  fi

                  echo "${display_name} job status: ${status}"

                  if [ "${status}" = "Succeeded" ]; then
                    echo "${display_name} completed successfully."
                    return 0
                  fi

                  if [ "${status}" = "Failed" ]; then
                    echo "${display_name} failed. Fetching recent job logs..." >&2
                    az containerapp job logs show \
                      --name "${job_name}" \
                      --resource-group "${RESOURCE_GROUP}" \
                      --execution "${execution}" \
                      --container "${container_name}" \
                      --tail 50 || true
                    return 1
                  fi

                  sleep 15
                done

                echo "Timed out waiting for ${display_name} job." >&2
                return 1
              }

              run_job "${POLICY_SEED_JOB_NAME}" policy-seed "Policy seed"

              echo "Waiting briefly before agent provisioning..."
              sleep 60

              run_job "${PROVISIONING_JOB_NAME}" agent-provisioning "Agent provisioning"
              echo "Post-deployment initialization completed successfully."
            '''
          ]
          environmentVariables: [
            {
              name: 'AZURE_CLIENT_ID'
              value: deploymentScriptIdentity.properties.clientId
            }
            {
              name: 'AZURE_SUBSCRIPTION_ID'
              value: subscription().subscriptionId
            }
            {
              name: 'RESOURCE_GROUP'
              value: resourceGroup().name
            }
            {
              name: 'POLICY_SEED_JOB_NAME'
              value: policySeedJobName
            }
            {
              name: 'PROVISIONING_JOB_NAME'
              value: provisioningJobName
            }
          ]
          resources: {
            requests: {
              cpu: 1
              memoryInGB: json('1.5')
            }
          }
        }
      }
    ]
    osType: 'Linux'
    restartPolicy: 'Never'
  }
  dependsOn: [
    deploymentScriptContributorRole
    deploymentScriptFoundryContributorRole
    deploymentScriptFoundryUserRole
  ]
}

resource runFabricSeed 'Microsoft.Resources/deploymentScripts@2023-08-01' = if (enableFabric) {
  name: 'run-fabric-seed-${deploymentSuffix}'
  location: location
  tags: resourceTags
  kind: 'AzurePowerShell'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${fabricUami!.id}': {}
    }
  }
  properties: {
    azPowerShellVersion: '11.0'
    retentionInterval: 'P1D'
    timeout: 'PT60M'
    cleanupPreference: 'OnSuccess'
    forceUpdateTag: deploymentSuffix
    scriptContent: loadTextContent('../scripts/seed-fabric-data.ps1')
    environmentVariables: [
      { name: 'AZURE_CLIENT_ID',         value: fabricUami!.properties.clientId }
      { name: 'FABRIC_WORKSPACE_ID',     value: fabricWorkspaceId }
      { name: 'FABRIC_WORKSPACE_NAME',   value: fabricWorkspaceName }
      { name: 'FABRIC_LAKEHOUSE_ID',     value: fabricLakehouseId }
      { name: 'FABRIC_LAKEHOUSE_NAME',   value: fabricLakehouseName }
      { name: 'REPOSITORY_ARCHIVE_URL',  value: fabricRepositoryArchiveUrl }
    ]
  }
  dependsOn: [
  ]
}

output fabricSeedDeploymentScriptName string = enableFabric ? runFabricSeed.name : ''
output postDeployRunnerName string = postDeployRunner.name
