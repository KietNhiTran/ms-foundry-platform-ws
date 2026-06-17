// ---------------------------------------------------------------------------
// Module 1 — Foundry Platform Overview & Setup
// Provisions: Foundry resource (AI Services) + GPT-4.1 model deployment
//             + Hub, Project, and Connection (for portal playground access)
// ---------------------------------------------------------------------------

targetScope = 'resourceGroup'

// ── Parameters ──────────────────────────────────────────────────────────────

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Unique name for the Foundry (AI Services) account. Must be globally unique.')
param accountName string

@description('Name of the AI Hub workspace.')
param hubName string = '${accountName}-hub'

@description('Name of the AI Project within the Hub.')
param projectName string = 'contoso-estimator'

@description('Chat model to deploy.')
param modelName string = 'gpt-4.1'

@description('Model version to deploy.')
param modelVersion string = '2025-04-14'

@description('Model deployment SKU.')
@allowed([
  'GlobalStandard'
  'DataZoneStandard'
  'Standard'
])
param modelSku string = 'GlobalStandard'

@description('Tokens-per-minute capacity (in thousands) for the model deployment.')
param modelCapacity int = 10

@description('Principal ID of the user to assign the Foundry User role.')
param principalId string = ''

// ── Foundry Resource (AI Services Account) ──────────────────────────────────

resource aiServices 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: accountName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
    disableLocalAuth: true
  }
}

// ── Model Deployment (GPT-4.1) ──────────────────────────────────────────────

resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-04-01-preview' = {
  parent: aiServices
  name: modelName
  sku: {
    name: modelSku
    capacity: modelCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }
    raiPolicyName: 'Microsoft.DefaultV2'
  }
}

// ── Supporting Resources for Hub / Project ───────────────────────────────────

var sanitizedName = replace(hubName, '-', '')

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take('st${sanitizedName}', 24)
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: { enabled: true }
        file: { enabled: true }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('kv-${sanitizedName}', 24)
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableRbacAuthorization: true
    accessPolicies: []
  }
}

// ── AI Hub ───────────────────────────────────────────────────────────────────

resource hub 'Microsoft.MachineLearningServices/workspaces@2024-04-01' = {
  name: hubName
  location: location
  kind: 'Hub'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    friendlyName: hubName
    storageAccount: storageAccount.id
    keyVault: keyVault.id
  }
}

// ── AI Project ──────────────────────────────────────────────────────────────

resource project 'Microsoft.MachineLearningServices/workspaces@2024-04-01' = {
  name: projectName
  location: location
  kind: 'Project'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    friendlyName: projectName
    hubResourceId: hub.id
  }
}

// ── Connection: Hub → Foundry Resource ──────────────────────────────────────

resource aiServicesConnection 'Microsoft.MachineLearningServices/workspaces/connections@2024-04-01' = {
  parent: hub
  name: accountName
  properties: {
    category: 'AIServices'
    target: 'https://${accountName}.services.ai.azure.com'
    authType: 'AAD'
    metadata: {
      ApiType: 'Azure'
      ResourceId: aiServices.id
    }
  }
}

// ── RBAC: Foundry User (Cognitive Services User) ────────────────────────────

// Role: Cognitive Services User — a97b65f3-24c7-4388-baec-2e87135dc908
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(aiServices.id, principalId, 'CognitiveServicesUser')
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
    principalId: principalId
    principalType: 'User'
  }
}

// ── Outputs ─────────────────────────────────────────────────────────────────

output foundryEndpoint string = 'https://${accountName}.services.ai.azure.com'
output foundryResourceId string = aiServices.id
output hubId string = hub.id
output projectId string = project.id
output modelDeploymentName string = modelDeployment.name
