# Teardown Module 1 infrastructure
# Usage: .\teardown.ps1 -ResourceGroupName "rg-contoso-foundry-workshop"

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName
)

$ErrorActionPreference = "Stop"

Write-Host "=== Teardown Module 1 ===" -ForegroundColor Red
Write-Host "This will DELETE the resource group: $ResourceGroupName" -ForegroundColor Red
Write-Host "All resources inside will be permanently removed." -ForegroundColor Red
Write-Host ""

$confirm = Read-Host "Type the resource group name to confirm deletion"
if ($confirm -ne $ResourceGroupName) {
    Write-Host "Confirmation failed. Aborting." -ForegroundColor Yellow
    exit 0
}

Write-Host "Deleting resource group..." -ForegroundColor Yellow
az group delete --name $ResourceGroupName --yes --no-wait

Write-Host "Deletion initiated (running in background)." -ForegroundColor Green
Write-Host "Check Azure Portal to confirm removal."
