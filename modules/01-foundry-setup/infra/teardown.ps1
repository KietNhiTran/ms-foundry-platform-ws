<#
.SYNOPSIS
    Tear down all resources created by Module 1.

.DESCRIPTION
    Deletes the resource group and all contained resources provisioned by deploy.ps1.
    Prompts for confirmation before deleting.

.PARAMETER ResourceGroupName
    Name of the resource group to delete.

.PARAMETER Force
    Skip confirmation prompt.

.EXAMPLE
    ./teardown.ps1 -ResourceGroupName "rg-foundry-workshop"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "=== Module 1: Teardown ===" -ForegroundColor Cyan
Write-Host ""

# Check if resource group exists
$rgExists = az group exists --name $ResourceGroupName --output tsv
if ($rgExists -ne "true") {
    Write-Host "Resource group '$ResourceGroupName' does not exist. Nothing to delete." -ForegroundColor Yellow
    exit 0
}

# Confirm deletion
if (-not $Force) {
    Write-Host "This will permanently delete resource group '$ResourceGroupName' and ALL resources inside it." -ForegroundColor Red
    $confirm = Read-Host "Type the resource group name to confirm"
    if ($confirm -ne $ResourceGroupName) {
        Write-Host "Confirmation failed. Aborting." -ForegroundColor Yellow
        exit 1
    }
}

# Delete resource group
Write-Host "Deleting resource group '$ResourceGroupName'..." -ForegroundColor Yellow
az group delete --name $ResourceGroupName --yes --no-wait
if ($LASTEXITCODE -ne 0) { throw "Failed to initiate resource group deletion." }

Write-Host ""
Write-Host "Resource group deletion initiated (runs in background)." -ForegroundColor Green
Write-Host "You can monitor progress in the Azure portal under Resource Groups." -ForegroundColor White
Write-Host ""
Write-Host "=== Teardown Complete ===" -ForegroundColor Cyan
