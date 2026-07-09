<#
.SYNOPSIS
    Assign a least-privilege RBAC role to a Foundry agent identity (or project
    managed identity) on a downstream Azure resource.

.DESCRIPTION
    Wraps `az role assignment create` with the correct flags for agent-identity
    service principals (--assignee-object-id + --assignee-principal-type
    ServicePrincipal), which avoids Microsoft Graph lookup errors.

    Module 12 — Secure Agent Identity & Access.

.EXAMPLE
    ./assign-agent-identity-rbac.ps1 `
      -AgentIdentityId "00000000-0000-0000-0000-000000000000" `
      -Role "Storage Blob Data Reader" `
      -Scope "/subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.Storage/storageAccounts/<sa>"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $AgentIdentityId,

    [Parameter(Mandatory = $true)]
    [string] $Role,

    [Parameter(Mandatory = $true)]
    [string] $Scope,

    # Optional Entra tenant to target (defaults to current az context).
    [string] $Tenant
)

$ErrorActionPreference = 'Stop'

if ($Tenant) {
    Write-Host "Setting az context to tenant $Tenant ..." -ForegroundColor Cyan
    az account set --only-show-errors 2>$null | Out-Null
}

Write-Host "Assigning role '$Role' to agent identity $AgentIdentityId" -ForegroundColor Cyan
Write-Host "  scope: $Scope" -ForegroundColor DarkGray

az role assignment create `
    --assignee-object-id $AgentIdentityId `
    --assignee-principal-type ServicePrincipal `
    --role $Role `
    --scope $Scope `
    --only-show-errors | Out-Null

Write-Host "Verifying ..." -ForegroundColor Cyan
az role assignment list `
    --assignee $AgentIdentityId `
    --scope $Scope `
    --only-show-errors `
    --output table

Write-Host "Done. The agent identity now has least-privilege '$Role' on the target." -ForegroundColor Green
