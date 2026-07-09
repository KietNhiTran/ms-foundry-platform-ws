<#
.SYNOPSIS
    Remove Module 13 demo artifacts: private endpoint, private DNS zone,
    Key Vault, and the CMK identity. Key Vault has purge protection, so it will
    be soft-deleted and retained for the configured window.

.EXAMPLE
    ./teardown.ps1 -ResourceGroup rg-contoso-foundry-workshop `
      -KeyVaultName kv-contoso-cmk -FoundryAccountName contoso-foundry-ws-resource
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ResourceGroup,
    [Parameter(Mandatory = $true)] [string] $KeyVaultName,
    [Parameter(Mandatory = $true)] [string] $FoundryAccountName,
    [string] $IdentityName = 'id-foundry-cmk'
)

$ErrorActionPreference = 'Continue'

$privateEndpointName = "$FoundryAccountName-pe"

Write-Host "Deleting private endpoint $privateEndpointName ..." -ForegroundColor Cyan
az network private-endpoint delete --name $privateEndpointName --resource-group $ResourceGroup --only-show-errors 2>$null

Write-Host "Deleting private DNS zone links + zone ..." -ForegroundColor Cyan
az network private-dns zone delete --name 'privatelink.cognitiveservices.azure.com' `
    --resource-group $ResourceGroup --yes --only-show-errors 2>$null

Write-Host "Deleting managed identity $IdentityName ..." -ForegroundColor Cyan
az identity delete --name $IdentityName --resource-group $ResourceGroup --only-show-errors 2>$null

Write-Host "Deleting Key Vault $KeyVaultName (soft-delete; purge protection retains it) ..." -ForegroundColor Cyan
az keyvault delete --name $KeyVaultName --resource-group $ResourceGroup --only-show-errors 2>$null

Write-Host "Teardown complete. Note: purge protection prevents immediate Key Vault purge." -ForegroundColor Green
