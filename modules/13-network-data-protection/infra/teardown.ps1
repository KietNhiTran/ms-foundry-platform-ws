<#
.SYNOPSIS
    Tear down the Module 13 network-secured environment created from Foundry
    sample Template 15.

.DESCRIPTION
    IMPORTANT (from the template's cleanup guidance): the delegated agent subnet
    stays linked to the account capability host until the Foundry account is
    PURGED — not just deleted. Simply deleting the resource group leaves the
    subnet in a "Subnet already in use" state and blocks reuse.

    This script:
      1. Deletes the resource group (removes all resources).
      2. Purges the soft-deleted Foundry (Cognitive Services) account so the
         platform unlinks the capability host and agent subnet (~20 min).

    For VNet-injection deployments the sample also ships a cleanup tool that
    handles the exact deletion order (project caphost -> account caphost ->
    purge -> SAL wait). See:
    https://github.com/microsoft-foundry/foundry-samples/blob/main/infrastructure/infrastructure-setup-bicep/deployment-tools/cleanup/README.md

.EXAMPLE
    ./teardown.ps1 -ResourceGroup rg-contoso-foundry-secured -Location eastus2
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = 'rg-contoso-foundry-secured',
    [string] $Location = 'eastus2',
    # Base name used for the Foundry account (aiServices param in main.bicepparam).
    # The template appends a deterministic suffix, so we discover the account name.
    [string] $AccountNamePrefix = 'contosofdrysec'
)

$ErrorActionPreference = 'Continue'

# --- Discover the network-secured Foundry account before deleting the RG ---
Write-Host "Discovering Foundry account in $ResourceGroup ..." -ForegroundColor Cyan
$accountName = az cognitiveservices account list `
    --resource-group $ResourceGroup `
    --query "[?kind=='AIServices'].name | [0]" -o tsv 2>$null

# --- Delete the resource group ---
Write-Host "Deleting resource group $ResourceGroup ..." -ForegroundColor Cyan
az group delete --name $ResourceGroup --yes --no-wait --only-show-errors 2>$null

# --- Purge the soft-deleted account so the capability host + agent subnet unlink ---
if ($accountName) {
    Write-Host "Purging soft-deleted Foundry account '$accountName' (unlinks agent subnet, ~20 min) ..." -ForegroundColor Cyan
    az cognitiveservices account purge `
        --name $accountName `
        --resource-group $ResourceGroup `
        --location $Location `
        --only-show-errors 2>$null
}
else {
    Write-Host "No AIServices account found; if reuse fails later, purge it manually:" -ForegroundColor Yellow
    Write-Host "  az cognitiveservices account purge --name <acct> --resource-group $ResourceGroup --location $Location" -ForegroundColor Yellow
}

Write-Host "Teardown initiated. Allow up to ~20 min for the capability host and agent subnet to fully unlink." -ForegroundColor Green
