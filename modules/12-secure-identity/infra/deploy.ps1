<#
.SYNOPSIS
    Deploy the Module 12 least-privilege identity sample (user-assigned managed
    identity + read-only Storage role assignment).

.EXAMPLE
    ./deploy.ps1 -ResourceGroup rg-contoso-foundry-workshop -StorageAccountName contosofoundrysa
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ResourceGroup,
    [Parameter(Mandatory = $true)] [string] $StorageAccountName,
    [string] $IdentityName = 'id-contoso-estimator-agent',
    [string] $Location = 'eastus2'
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "Deploying least-privilege agent identity sample to $ResourceGroup ..." -ForegroundColor Cyan

az deployment group create `
    --resource-group $ResourceGroup `
    --template-file (Join-Path $here 'main.bicep') `
    --parameters `
        location=$Location `
        identityName=$IdentityName `
        storageAccountName=$StorageAccountName `
    --only-show-errors `
    --query "properties.outputs" `
    --output json

Write-Host "Done. Use the identityPrincipalId output with assign-agent-identity-rbac.ps1 to add more scoped roles." -ForegroundColor Green
