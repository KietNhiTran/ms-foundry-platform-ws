<#
.SYNOPSIS
    Creates the Contoso-Eval-SP service principal for CI/CD evaluation workflows.

.DESCRIPTION
    Sets up a service principal used by GitHub Actions to trigger automated agent
    evaluations. The script:
      1. Creates an Entra ID app registration "Contoso-Eval-SP"
      2. Creates a client secret (configurable expiry)
      3. Assigns "Reader" role at subscription scope (for az login)
      4. Assigns "Foundry User" role on the resource group (falls back to legacy
         "Azure AI User" name if tenant rename hasn't rolled out)
      5. Assigns "Cognitive Services OpenAI User" role on the resource group
      6. Assigns "Storage Blob Data Contributor" role on the resource group
      7. Prints all values needed for GitHub Actions secrets

    Re-running is safe -- each step checks if the resource already exists.

.PARAMETER ResourceGroup
    Azure resource group name containing Foundry resources.

.PARAMETER SubscriptionId
    Azure subscription ID.

.PARAMETER SecretExpiryDays
    Client secret validity in days. Default: 365

.EXAMPLE
    .\setup_eval_sp.ps1 -ResourceGroup "my-rg" -SubscriptionId "00000000-0000-0000-0000-000000000000"
    .\setup_eval_sp.ps1 -ResourceGroup "my-rg" -SubscriptionId "00000000-0000-0000-0000-000000000000" -SecretExpiryDays 90
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$SubscriptionId,

    [string]$AppName = "Contoso-Eval-SP",
    [int]$SecretExpiryDays = 365
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Contoso-Eval-SP Setup -- CI/CD Evaluation Service Principal"
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  App Name:        $AppName"
Write-Host "  Resource Group:  $ResourceGroup"
Write-Host "  Subscription:    $SubscriptionId"
Write-Host "  Secret Expiry:   $SecretExpiryDays days"
Write-Host ""

# -- Ensure correct subscription ----------------------------------
Write-Host "[1/5] Setting subscription..." -ForegroundColor Yellow
az account set --subscription $SubscriptionId
$tenantId = (az account show --query "tenantId" --output tsv)
Write-Host "  Tenant: $tenantId"

# -- Step 1: Create app registration (idempotent) -----------------
Write-Host ""
Write-Host "[2/5] Creating app registration '$AppName'..." -ForegroundColor Yellow

$existingApp = az ad app list --display-name $AppName --query "[0].appId" --output tsv 2>$null

if ($existingApp) {
    Write-Host "  App already exists: $existingApp"
    $appId = $existingApp
} else {
    $appId = az ad app create --display-name $AppName --query "appId" --output tsv
    Write-Host "  Created app: $appId"
}

# -- Step 2: Ensure service principal exists ----------------------
Write-Host ""
Write-Host "[3/5] Ensuring service principal exists..." -ForegroundColor Yellow

$spObjectId = az ad sp list --filter "appId eq '$appId'" --query "[0].id" --output tsv 2>$null

if ($spObjectId) {
    Write-Host "  SP already exists: $spObjectId"
} else {
    $spObjectId = az ad sp create --id $appId --query "id" --output tsv
    Write-Host "  Created SP: $spObjectId"
    Write-Host "  Waiting 10s for Entra propagation..."
    Start-Sleep -Seconds 10
}

# -- Step 3: Create client secret ---------------------------------
Write-Host ""
Write-Host "[4/5] Creating client secret ($SecretExpiryDays days)..." -ForegroundColor Yellow

$endDate = (Get-Date).AddDays($SecretExpiryDays).ToString("yyyy-MM-ddTHH:mm:ssZ")
$secretResult = az ad app credential reset `
    --id $appId `
    --append `
    --display-name "github-actions-eval" `
    --end-date $endDate `
    --query "{clientId:appId, clientSecret:password, tenantId:tenant}" `
    --output json | ConvertFrom-Json

$clientSecret = $secretResult.clientSecret
Write-Host "  Secret created (expires: $endDate)"
Write-Host "  WARNING: The secret value is shown ONCE below. Save it now." -ForegroundColor Red

# -- Step 4: Assign Azure RBAC roles ------------------------------
Write-Host ""
Write-Host "[5/5] Assigning RBAC roles on resource group '$ResourceGroup'..." -ForegroundColor Yellow

# Foundry User is the modern role for agent operations (agents/*/read, action, delete).
# Some tenants still show the previous name "Azure AI User" while the rename rolls out
# (role ID and permissions are identical). We try the new name first, fall back to old.
# Avoid "Azure AI Developer" -- MS docs warn against using it for Foundry work.
$foundryUserRole = "Foundry User"
$foundryUserExists = az role definition list --name $foundryUserRole --query "[0].roleName" --output tsv
if (-not $foundryUserExists) {
    $foundryUserRole = "Azure AI User"
    Write-Host "  Note: 'Foundry User' not found in tenant, using legacy name 'Azure AI User'"
}

$roles = @($foundryUserRole, "Cognitive Services OpenAI User", "Storage Blob Data Contributor")
$scope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup"

# Reader at subscription level -- required for az login to enumerate subscriptions
$subScope = "/subscriptions/$SubscriptionId"
$existingReaderJson = az role assignment list `
    --assignee-object-id $spObjectId `
    --role "Reader" `
    --scope $subScope `
    --output json | ConvertFrom-Json

if ($existingReaderJson.Count -gt 0) {
    Write-Host "  'Reader' (subscription) already assigned"
} else {
    az role assignment create `
        --assignee-object-id $spObjectId `
        --assignee-principal-type ServicePrincipal `
        --role "Reader" `
        --scope $subScope `
        --output none
    Write-Host "  Assigned 'Reader' at subscription scope"
}

foreach ($role in $roles) {
    $existingJson = az role assignment list `
        --assignee-object-id $spObjectId `
        --role $role `
        --scope $scope `
        --output json | ConvertFrom-Json

    if ($existingJson.Count -gt 0) {
        Write-Host "  '$role' already assigned"
    } else {
        az role assignment create `
            --assignee-object-id $spObjectId `
            --assignee-principal-type ServicePrincipal `
            --role $role `
            --scope $scope `
            --output none
        Write-Host "  Assigned '$role'"
    }
}

# -- Summary ------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Contoso-Eval-SP Setup Complete"
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  App Name:         $AppName"
Write-Host "  Client ID:        $appId"
Write-Host "  Client Secret:    $clientSecret" -ForegroundColor Yellow
Write-Host "  Tenant ID:        $tenantId"
Write-Host "  SP Object ID:     $spObjectId"
Write-Host "  RBAC Roles:       $($roles -join ', ')"
Write-Host "  Scope:            $scope"
Write-Host "  Secret Expires:   $endDate"
Write-Host ""
Write-Host "------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Add these as GitHub Actions secrets:"
Write-Host "------------------------------------------------------------" -ForegroundColor Cyan
Write-Host ""
Write-Host "  AZURE_CLIENT_ID=$appId"
Write-Host "  AZURE_CLIENT_SECRET=$clientSecret"
Write-Host "  AZURE_TENANT_ID=$tenantId"
Write-Host "  AZURE_SUBSCRIPTION_ID=$SubscriptionId"
Write-Host "  AZURE_AI_PROJECT_ENDPOINT=<your-foundry-project-endpoint>"
Write-Host ""
Write-Host "  IMPORTANT: Save the client secret NOW." -ForegroundColor Red
Write-Host "  It cannot be retrieved later -- only regenerated." -ForegroundColor Red
Write-Host "============================================================" -ForegroundColor Cyan
