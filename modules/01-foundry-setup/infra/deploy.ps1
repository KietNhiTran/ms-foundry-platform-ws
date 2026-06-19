# Deploy Module 1 infrastructure
# Usage: .\deploy.ps1 -ResourceGroupName "rg-contoso-foundry-workshop" -Location "australiaeast" -FoundryResourceName "contoso-foundry-ws"

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory=$false)]
    [string]$Location = "australiaeast",

    [Parameter(Mandatory=$true)]
    [string]$FoundryResourceName
)

$ErrorActionPreference = "Stop"

Write-Host "=== Module 1: Foundry Platform Setup ===" -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName"
Write-Host "Location: $Location"
Write-Host "Foundry Resource: $FoundryResourceName"
Write-Host ""

# Create resource group if it doesn't exist
Write-Host "Creating resource group..." -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location --output none

# Deploy Bicep template
Write-Host "Deploying Foundry resource + GPT-4.1 model..." -ForegroundColor Yellow
$output = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file "$PSScriptRoot\main.bicep" `
    --parameters foundryResourceName=$FoundryResourceName `
    --output json

if ($LASTEXITCODE -ne 0) {
    Write-Host "Deployment failed!" -ForegroundColor Red
    exit 1
}

$result = $output | ConvertFrom-Json

$endpoint = $result.properties.outputs.endpoint.value
Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Green
Write-Host "Project Endpoint: $endpoint"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Open https://ai.azure.com and verify the project"
Write-Host "  2. Test GPT-4.1 in the playground"
Write-Host "  3. Proceed to Module 2 to create the agent"
