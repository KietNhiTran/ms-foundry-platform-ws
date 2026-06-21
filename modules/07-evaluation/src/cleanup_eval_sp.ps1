<#
.SYNOPSIS
    Removes the Contoso-Eval-SP service principal and all its assignments.

.DESCRIPTION
    Tears down everything created by setup_eval_sp.ps1:
      1. Removes RBAC role assignments from the resource group
      2. Deletes the service principal
      3. Deletes the app registration

    Safe to run if the SP doesn't exist — each step checks first.

.PARAMETER ResourceGroup
    Azure resource group name. Must match what was used in setup.

.PARAMETER SubscriptionId
    Azure subscription ID.

.EXAMPLE
    .\cleanup_eval_sp.ps1 -ResourceGroup "my-rg" -SubscriptionId "00000000-0000-0000-0000-000000000000"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$SubscriptionId,

    [string]$AppName = "Contoso-Eval-SP"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Contoso-Eval-SP Cleanup"
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

az account set --subscription $SubscriptionId

# Find the app
$appId = az ad app list --display-name $AppName --query "[0].appId" --output tsv 2>$null

if (-not $appId) {
    Write-Host "  App '$AppName' not found. Nothing to clean up." -ForegroundColor Yellow
    return
}

Write-Host "  Found app: $appId"
$spObjectId = az ad sp list --filter "appId eq '$appId'" --query "[0].id" --output tsv 2>$null

# ── Remove RBAC roles ────────────────────────────────────────────
Write-Host ""
Write-Host "[1/3] Removing RBAC role assignments..." -ForegroundColor Yellow
$scope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup"

$assignments = az role assignment list --assignee $appId --scope $scope --query "[].roleDefinitionName" --output tsv 2>$null
if ($assignments) {
    foreach ($role in ($assignments -split "`n")) {
        $role = $role.Trim()
        if ($role) {
            az role assignment delete --assignee $appId --role $role --scope $scope --output none 2>$null
            Write-Host "  Removed '$role'"
        }
    }
} else {
    Write-Host "  No role assignments found"
}

# ── Delete service principal ─────────────────────────────────────
Write-Host ""
Write-Host "[2/3] Deleting service principal..." -ForegroundColor Yellow

if ($spObjectId) {
    az ad sp delete --id $spObjectId 2>$null
    Write-Host "  Deleted SP: $spObjectId"
} else {
    Write-Host "  SP not found, skipping"
}

# ── Delete app registration ──────────────────────────────────────
Write-Host ""
Write-Host "[3/3] Deleting app registration..." -ForegroundColor Yellow
az ad app delete --id $appId 2>$null
Write-Host "  Deleted app: $appId"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Cleanup complete. Contoso-Eval-SP removed."
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Remember to also remove these GitHub Actions secrets:"
Write-Host "  - AZURE_CLIENT_ID"
Write-Host "  - AZURE_CLIENT_SECRET"
Write-Host "  - AZURE_TENANT_ID"
Write-Host "  - AZURE_AI_PROJECT_ENDPOINT"
Write-Host ""
