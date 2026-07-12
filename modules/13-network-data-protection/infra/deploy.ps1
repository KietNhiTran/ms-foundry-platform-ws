<#
.SYNOPSIS
    Deploy Module 13's network-secured environment using the OFFICIAL Foundry
    sample "Template 15 — Standard Agent Setup with E2E Network Isolation".

    Instead of maintaining our own private-networking Bicep, this script:
      1. Sparse-clones template 15 from microsoft-foundry/foundry-samples.
      2. Copies this folder's main.bicepparam into the downloaded template.
      3. Runs `az deployment group create` against the official main.bicep.

    Source: https://github.com/microsoft-foundry/foundry-samples/tree/main/
            infrastructure/infrastructure-setup-bicep/15-private-network-standard-agent-setup

.DESCRIPTION
    Deploys (fresh) into a DEDICATED resource group so the public Module 1
    Foundry resource is left untouched:
      - New VNet (agent subnet delegated to Microsoft.App/environments + PE subnet)
      - Network-secured Foundry account (public network access DISABLED) + project
      - BYO Storage / Cosmos DB / AI Search behind private endpoints
      - Azure Monitor Private Link Scope (AMPLS) for private telemetry
      - Private DNS zones + capability host (created implicitly by the platform)

    Requires: Azure CLI, git, and the resource providers registered
    (Microsoft.KeyVault, CognitiveServices, Storage, Search, Network, App,
    ContainerService). See ../README.md Pre-Demo Checklist.

.EXAMPLE
    ./deploy.ps1 -ResourceGroup rg-contoso-foundry-secured -Location eastus2
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = 'rg-contoso-foundry-secured',
    [string] $Location = 'eastus2',
    # Branch/ref of foundry-samples to pull the template from.
    [string] $Ref = 'main',
    # Skip the git sparse-clone if the template was already downloaded.
    [switch] $SkipDownload
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

$repoUrl = 'https://github.com/microsoft-foundry/foundry-samples.git'
$templateRelPath = 'infrastructure/infrastructure-setup-bicep/15-private-network-standard-agent-setup'
$workDir = Join-Path $here '.template-15'
$templateDir = Join-Path $workDir $templateRelPath

# --- 1. Fetch the official template (sparse checkout of just template 15) ---
if (-not $SkipDownload) {
    if (Test-Path $workDir) {
        Write-Host "Refreshing existing template checkout at $workDir ..." -ForegroundColor Cyan
        git -C $workDir fetch --depth 1 origin $Ref | Out-Null
        git -C $workDir checkout $Ref | Out-Null
        git -C $workDir pull --depth 1 origin $Ref | Out-Null
    }
    else {
        Write-Host "Sparse-cloning template 15 from foundry-samples ($Ref) ..." -ForegroundColor Cyan
        git clone --depth 1 --filter=blob:none --sparse --branch $Ref $repoUrl $workDir | Out-Null
        git -C $workDir sparse-checkout set $templateRelPath | Out-Null
    }
}

if (-not (Test-Path (Join-Path $templateDir 'main.bicep'))) {
    throw "Template not found at $templateDir. Re-run without -SkipDownload."
}

# --- 2. Stage our workshop parameters alongside the official template ---
Write-Host "Staging workshop parameters ..." -ForegroundColor Cyan
Copy-Item (Join-Path $here 'main.bicepparam') (Join-Path $templateDir 'main.bicepparam') -Force

# --- 3. Ensure the resource group exists ---
Write-Host "Ensuring resource group $ResourceGroup ($Location) ..." -ForegroundColor Cyan
az group create --name $ResourceGroup --location $Location --only-show-errors | Out-Null

# --- 4. Deploy the official template ---
Write-Host "Deploying network-secured Standard Agent environment ..." -ForegroundColor Cyan
Write-Host "  (private endpoints + VNet injection can take 15-25 min)" -ForegroundColor DarkGray

az deployment group create `
    --resource-group $ResourceGroup `
    --template-file (Join-Path $templateDir 'main.bicep') `
    --parameters (Join-Path $templateDir 'main.bicepparam') `
    --parameters location=$Location `
    --only-show-errors `
    --query "properties.outputs" `
    --output json

Write-Host "Done. Access the private Foundry resource from inside the VNet (jump box / VPN / Bastion)." -ForegroundColor Green
Write-Host "Teardown with ./teardown.ps1 -ResourceGroup $ResourceGroup (purges the account so the agent subnet can be reused)." -ForegroundColor DarkGray
