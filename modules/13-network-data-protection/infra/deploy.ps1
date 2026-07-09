<#
.SYNOPSIS
    Deploy the Module 13 CMK + private-networking prerequisites sample:
    Key Vault (purge protection) + RSA key + identity (Key Vault Crypto User)
    + private endpoint/DNS to an existing Foundry account.

.EXAMPLE
    ./deploy.ps1 -ResourceGroup rg-contoso-foundry-workshop `
      -KeyVaultName kv-contoso-cmk -FoundryAccountName contoso-foundry-ws-resource `
      -VnetName vnet-contoso -SubnetName snet-foundry
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ResourceGroup,
    [Parameter(Mandatory = $true)] [string] $KeyVaultName,
    [Parameter(Mandatory = $true)] [string] $FoundryAccountName,
    [Parameter(Mandatory = $true)] [string] $VnetName,
    [Parameter(Mandatory = $true)] [string] $SubnetName,
    [string] $Location = 'eastus2'
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "Deploying CMK + private-networking sample to $ResourceGroup ..." -ForegroundColor Cyan

az deployment group create `
    --resource-group $ResourceGroup `
    --template-file (Join-Path $here 'main.bicep') `
    --parameters `
        location=$Location `
        keyVaultName=$KeyVaultName `
        foundryAccountName=$FoundryAccountName `
        vnetName=$VnetName `
        subnetName=$SubnetName `
    --only-show-errors `
    --query "properties.outputs" `
    --output json

Write-Host "Done. Enable CMK on the Foundry resource's Encryption blade using the key vault + key created here." -ForegroundColor Green
