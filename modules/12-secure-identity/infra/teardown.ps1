<#
.SYNOPSIS
    Remove Module 12 demo artifacts: the read-only role assignment and the
    user-assigned managed identity.

.EXAMPLE
    ./teardown.ps1 -ResourceGroup rg-contoso-foundry-workshop -StorageAccountName contosofoundrysa
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ResourceGroup,
    [Parameter(Mandatory = $true)] [string] $StorageAccountName,
    [string] $IdentityName = 'id-contoso-estimator-agent'
)

$ErrorActionPreference = 'Stop'

$principalId = az identity show --name $IdentityName --resource-group $ResourceGroup `
    --query principalId -o tsv --only-show-errors 2>$null

if ($principalId) {
    $scope = az storage account show --name $StorageAccountName --resource-group $ResourceGroup `
        --query id -o tsv --only-show-errors
    Write-Host "Removing role assignments for $principalId on $StorageAccountName ..." -ForegroundColor Cyan
    az role assignment delete --assignee $principalId --scope $scope --only-show-errors 2>$null
}

Write-Host "Deleting managed identity $IdentityName ..." -ForegroundColor Cyan
az identity delete --name $IdentityName --resource-group $ResourceGroup --only-show-errors 2>$null

Write-Host "Teardown complete." -ForegroundColor Green
