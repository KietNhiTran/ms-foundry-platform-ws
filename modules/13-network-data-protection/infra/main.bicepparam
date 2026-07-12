// -----------------------------------------------------------------------------
// Module 13 — parameters for Foundry sample TEMPLATE 15
// "Standard Agent Setup with E2E Network Isolation (without Tools behind VNet)"
//
// This file is copied into the downloaded template folder by deploy.ps1, so
// `using './main.bicep'` resolves to the official template's main.bicep.
// Source: https://github.com/microsoft-foundry/foundry-samples/tree/main/
//         infrastructure/infrastructure-setup-bicep/15-private-network-standard-agent-setup
//
// Fresh, self-contained deployment: a new network-secured Foundry account +
// project + BYO Storage / Cosmos DB / AI Search, all behind private endpoints
// in a customer-owned VNet. Deploy into a DEDICATED resource group so the
// public Module 1 resource is left untouched.
// -----------------------------------------------------------------------------
using './main.bicep'

// --- Core ---
param location = 'eastus2'
param aiServices = 'contosofdrysec'
param firstProjectName = 'contoso-secured'
param displayName = 'Contoso Estimator (Secured)'
param projectDescription = 'Network-secured Foundry project for the Contoso Estimator demo'

// --- Model deployment ---
param modelName = 'gpt-4.1'
param modelFormat = 'OpenAI'
param modelVersion = '2025-04-14'
param modelSkuName = 'GlobalStandard'
param modelCapacity = 30

// --- Network (create a NEW VNet — Class C 192.168.0.0/16 works in every region) ---
param existingVnetResourceId = ''
param vnetName = 'vnet-contoso-secured'
param vnetAddressPrefix = '192.168.0.0/16'
param agentSubnetName = 'agent-subnet'
param agentSubnetPrefix = '192.168.0.0/24'
param peSubnetName = 'pe-subnet'
param peSubnetPrefix = '192.168.1.0/24'

// --- BYO dependencies (leave empty to let the template create them) ---
param aiSearchResourceId = ''
param azureStorageAccountResourceId = ''
param azureCosmosDBAccountResourceId = ''

// --- Container Registry (off to keep the demo lean; ACR Premium adds cost and
//     is only needed for custom container-based tools) ---
param enableContainerRegistry = false

// --- Capability host: leave false for a fresh deploy — the platform creates it
//     implicitly via networkInjections.scenario='agent'. ---
param createAccountCapabilityHost = false

// --- Private DNS zones (leave empty to create new zones in this subscription) ---
param dnsZonesSubscriptionId = ''
param existingDnsZones = {
  'privatelink.services.ai.azure.com': ''
  'privatelink.openai.azure.com': ''
  'privatelink.cognitiveservices.azure.com': ''
  'privatelink.search.windows.net': ''
  'privatelink.blob.core.windows.net': ''
  'privatelink.documents.azure.com': ''
}
param dnsZoneNames = [
  'privatelink.services.ai.azure.com'
  'privatelink.openai.azure.com'
  'privatelink.cognitiveservices.azure.com'
  'privatelink.search.windows.net'
  'privatelink.blob.core.windows.net'
  'privatelink.documents.azure.com'
]
