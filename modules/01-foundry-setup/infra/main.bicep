targetScope = 'resourceGroup'

@description('Name of the Foundry resource (AI Services account)')
param foundryResourceName string

@description('Azure region for deployment')
param location string = resourceGroup().location

@description('Name of the Foundry project')
param projectName string = 'contoso-estimator'

@description('Model deployment name')
param modelDeploymentName string = 'gpt-4o'

@description('Model name to deploy')
param modelName string = 'gpt-4o'

@description('Model version')
param modelVersion string = '2024-08-06'

@description('Tokens per minute limit')
param tpmLimit int = 30000

@description('Name of the Application Insights resource')
param appInsightsName string = '${foundryResourceName}-appinsights'

@description('Name of the Log Analytics workspace')
param logAnalyticsName string = '${foundryResourceName}-logs'

// Log Analytics Workspace (required by Application Insights)
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Application Insights for tracing
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// Foundry Resource (AI Services account)
resource foundryResource 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: foundryResourceName
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: foundryResourceName
    publicNetworkAccess: 'Enabled'
    allowProjectManagement: true
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// GPT-5.4 Model Deployment
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: foundryResource
  name: modelDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: tpmLimit / 1000 // Capacity is in K-TPM
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }
  }
}

// Foundry Project (child resource)
resource project 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: foundryResource
  name: projectName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

// Connect Application Insights to the Foundry resource
// Ref: https://github.com/microsoft-foundry/foundry-samples/tree/main/infrastructure/infrastructure-setup-bicep/01-connections
resource appInsightsConnection 'Microsoft.CognitiveServices/accounts/connections@2025-04-01-preview' = {
  name: '${foundryResourceName}-appinsights'
  parent: foundryResource
  properties: {
    category: 'AppInsights'
    target: appInsights.id
    authType: 'ApiKey'
    isSharedToAll: true
    credentials: {
      key: appInsights.properties.ConnectionString
    }
    metadata: {
      ApiType: 'Azure'
      ResourceId: appInsights.id
    }
  }
}

// Reader role definition ID: acdd72a7-3385-48ef-bd42-f606fba81ae7
var readerRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'acdd72a7-3385-48ef-bd42-f606fba81ae7')

// Assign the Foundry project's managed identity the Reader role on Application Insights
resource projectReaderOnAppInsights 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appInsights.id, project.id, readerRoleDefinitionId)
  scope: appInsights
  properties: {
    principalId: project.identity.principalId
    roleDefinitionId: readerRoleDefinitionId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output foundryResourceId string = foundryResource.id
output projectId string = project.id
output endpoint string = 'https://${foundryResourceName}.services.ai.azure.com/api/projects/${projectName}'
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output appInsightsResourceId string = appInsights.id
