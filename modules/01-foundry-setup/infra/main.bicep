targetScope = 'resourceGroup'

@description('Name of the Foundry resource (AI Services account)')
param foundryResourceName string

@description('Azure region for deployment')
param location string = resourceGroup().location

@description('Name of the Foundry project')
param projectName string = 'contoso-estimator'

@description('GPT-4.1 deployment name')
param modelDeploymentName string = 'gpt-4-1'

@description('Tokens per minute limit for GPT-4.1')
param tpmLimit int = 30000

// Foundry Resource (AI Services account)
resource foundryResource 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: foundryResourceName
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: foundryResourceName
    publicNetworkAccess: 'Enabled'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// GPT-4.1 Model Deployment
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundryResource
  name: modelDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: tpmLimit / 1000 // Capacity is in K-TPM
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4.1'
      version: '2025-04-14'
    }
  }
}

// Foundry Project (child resource)
resource project 'Microsoft.CognitiveServices/accounts/projects@2024-10-01' = {
  parent: foundryResource
  name: projectName
  location: location
  properties: {}
}

// Outputs
output foundryResourceId string = foundryResource.id
output projectId string = project.id
output endpoint string = 'https://${foundryResourceName}.services.ai.azure.com/api/projects/${projectName}'
