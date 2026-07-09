// -----------------------------------------------------------------------------
// Module 12 — Secure Agent Identity & Access
//
// Illustrative least-privilege pattern: a user-assigned managed identity that a
// (published) agent or client app can federate to, granted a single read-only
// data-plane role on an existing Storage account. Demonstrates that access is an
// explicit, auditable grant — not ambient.
//
// This is a teaching sample. In Foundry, published agents receive their own
// distinct Entra agent identity; use assign-agent-identity-rbac.ps1 to grant
// that identity the equivalent role. See ../README.md.
// -----------------------------------------------------------------------------

@description('Location for the managed identity.')
param location string = resourceGroup().location

@description('Name of the user-assigned managed identity to create.')
param identityName string = 'id-contoso-estimator-agent'

@description('Name of an EXISTING storage account to grant read access on.')
param storageAccountName string

// Role definition IDs (built-in)
var storageBlobDataReaderRoleId = '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'

resource agentIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

// Least-privilege: read-only blob access, scoped to this storage account only.
resource blobReaderAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, agentIdentity.id, storageBlobDataReaderRoleId)
  scope: storage
  properties: {
    principalId: agentIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataReaderRoleId)
  }
}

output identityName string = agentIdentity.name
output identityPrincipalId string = agentIdentity.properties.principalId
output identityClientId string = agentIdentity.properties.clientId
