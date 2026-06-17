<#
.SYNOPSIS
    Deploy Module 1 — Foundry resource, Hub, Project, and GPT-4.1 model deployment.

.DESCRIPTION
    One-click deployment script for the Foundry Platform Overview & Setup module.
    Creates all Azure resources needed for the workshop using the accompanying Bicep template.

.PARAMETER ResourceGroupName
    Name of the resource group to create or use.

.PARAMETER Location
    Azure region for all resources (default: eastus2).

.PARAMETER AccountName
    Globally unique name for the Foundry (AI Services) account.

.PARAMETER PrincipalId
    (Optional) Microsoft Entra Object ID of the presenter to assign the Foundry User role.

.EXAMPLE
    ./deploy.ps1 -ResourceGroupName "rg-foundry-workshop" -AccountName "foundryws2026"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false)]
    [string]$Location = "eastus2",

    [Parameter(Mandatory = $true)]
    [string]$AccountName,

    [Parameter(Mandatory = $false)]
    [string]$PrincipalId = ""
)

$ErrorActionPreference = "Stop"

Write-Host "=== Module 1: Foundry Platform Overview & Setup ===" -ForegroundColor Cyan
Write-Host ""

# 1. Ensure resource group exists
Write-Host "[1/3] Creating resource group '$ResourceGroupName' in '$Location'..." -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location --output none
if ($LASTEXITCODE -ne 0) { throw "Failed to create resource group." }
Write-Host "      Resource group ready." -ForegroundColor Green

# 2. Deploy Bicep template
Write-Host "[2/3] Deploying Foundry resources (this may take 3-5 minutes)..." -ForegroundColor Yellow

$templateFile = Join-Path $PSScriptRoot "main.bicep"

$deployParams = @(
    "deployment", "group", "create",
    "--resource-group", $ResourceGroupName,
    "--template-file", $templateFile,
    "--parameters", "accountName=$AccountName",
    "--parameters", "location=$Location"
)

if ($PrincipalId -ne "") {
    $deployParams += @("--parameters", "principalId=$PrincipalId")
}

$result = az @deployParams --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "Bicep deployment failed." }
Write-Host "      Deployment succeeded." -ForegroundColor Green

# 3. Display outputs
Write-Host "[3/3] Deployment outputs:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Foundry Endpoint  : $($result.properties.outputs.foundryEndpoint.value)" -ForegroundColor White
Write-Host "  Foundry Resource  : $($result.properties.outputs.foundryResourceId.value)" -ForegroundColor White
Write-Host "  Hub               : $($result.properties.outputs.hubId.value)" -ForegroundColor White
Write-Host "  Project           : $($result.properties.outputs.projectId.value)" -ForegroundColor White
Write-Host "  Model Deployment  : $($result.properties.outputs.modelDeploymentName.value)" -ForegroundColor White
Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Open https://ai.azure.com and verify the project appears" -ForegroundColor White
Write-Host "  2. Navigate to the Playground and select the '$($result.properties.outputs.modelDeploymentName.value)' deployment" -ForegroundColor White
Write-Host "  3. Send a test message to verify the model responds" -ForegroundColor White
