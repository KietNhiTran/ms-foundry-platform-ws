// -----------------------------------------------------------------------------
// Module 13 — Network Isolation & Data Protection
//
// Illustrative CMK + private-networking prerequisites for a Foundry resource:
//   - Key Vault with soft delete + purge protection (required for CMK)
//   - An RSA-2048 key
//   - A user-assigned identity granted Key Vault Crypto User (get/wrap/unwrap)
//   - A private endpoint + private DNS zone to an EXISTING Foundry account
//
// Teaching sample for a led demo. Enabling CMK on the Foundry account itself is
// done on the resource's Encryption blade (portal) or its encryption property.
// See ../README.md and docs/data-protection-cmk.md.
// -----------------------------------------------------------------------------

@description('Location for new resources.')
param location string = resourceGroup().location

@description('Name for the Key Vault that will hold the customer-managed key.')
param keyVaultName string

@description('Name of the RSA key to create for CMK.')
param keyName string = 'foundry-cmk'

@description('Name of the user-assigned identity to grant CMK permissions to.')
param identityName string = 'id-foundry-cmk'

@description('Name of an EXISTING Foundry (Cognitive Services) account to add a private endpoint to.')
param foundryAccountName string

@description('Name of an EXISTING virtual network for the private endpoint.')
param vnetName string

@description('Name of an EXISTING subnet (in vnetName) for the private endpoint.')
param subnetName string

// Built-in role: Key Vault Crypto User
var keyVaultCryptoUserRoleId = '12338af0-0e69-4776-bea7-57ae8d297424'

var privateDnsZoneName = 'privatelink.cognitiveservices.azure.com'
var privateEndpointName = '${foundryAccountName}-pe'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true // required for CMK
  }
}

resource cmkKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: keyName
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: [
      'wrapKey'
      'unwrapKey'
    ]
  }
}

resource cmkIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource cryptoUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, cmkIdentity.id, keyVaultCryptoUserRoleId)
  scope: keyVault
  properties: {
    principalId: cmkIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultCryptoUserRoleId)
  }
}

resource foundryAccount 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: foundryAccountName
}

resource subnet 'Microsoft.Network/virtualNetworks/subnets@2023-11-01' existing = {
  name: '${vnetName}/${subnetName}'
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: privateEndpointName
  location: location
  properties: {
    subnet: {
      id: subnet.id
    }
    privateLinkServiceConnections: [
      {
        name: privateEndpointName
        properties: {
          privateLinkServiceId: foundryAccount.id
          groupIds: [
            'account'
          ]
        }
      }
    ]
  }
}

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: privateDnsZoneName
  location: 'global'
}

resource dnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: privateDnsZone
  name: '${vnetName}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: resourceId('Microsoft.Network/virtualNetworks', vnetName)
    }
  }
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'config'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

output keyVaultUri string = keyVault.properties.vaultUri
output cmkKeyName string = cmkKey.name
output cmkIdentityPrincipalId string = cmkIdentity.properties.principalId
output privateEndpointId string = privateEndpoint.id
