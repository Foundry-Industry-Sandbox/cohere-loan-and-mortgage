$ErrorActionPreference = 'Stop'

$envWorkspaceName = $env:FABRIC_WORKSPACE_NAME
$envLakehouseName = $env:FABRIC_LAKEHOUSE_NAME
$envRepositoryArchiveUrl = $env:REPOSITORY_ARCHIVE_URL
$envGithubToken = $env:GITHUB_TOKEN

if ([string]::IsNullOrWhiteSpace($envWorkspaceName)) {
    throw 'FABRIC_WORKSPACE_NAME is required (set by Bicep runFabricSeed environment variables).'
}
if ([string]::IsNullOrWhiteSpace($envLakehouseName)) {
    throw 'FABRIC_LAKEHOUSE_NAME is required (set by Bicep runFabricSeed environment variables).'
}
if ([string]::IsNullOrWhiteSpace($envRepositoryArchiveUrl)) {
    throw 'REPOSITORY_ARCHIVE_URL is required (set by Bicep runFabricSeed from fabricRepositoryArchiveUrl).'
}

function Get-ScratchRoot {
    foreach ($candidate in @($env:TEMP, $env:TMPDIR, '/tmp')) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            return $candidate
        }
    }
    return '/tmp'
}

function Expand-ArchiveCrossPlatform {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArchivePath,
        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    try {
        Expand-Archive -LiteralPath $ArchivePath -DestinationPath $DestinationPath -Force
        return
    }
    catch {
        Write-Host "Expand-Archive failed, trying unzip: $($_.Exception.Message)"
    }

    $unzip = Get-Command unzip -ErrorAction SilentlyContinue
    if (-not $unzip) {
        throw 'Could not extract repository archive: Expand-Archive failed and unzip is unavailable.'
    }

    & $unzip.Source -o $ArchivePath -d $DestinationPath
    if ($LASTEXITCODE -ne 0) {
        throw "unzip failed with exit code $LASTEXITCODE"
    }
}

Write-Host '=== Fabric Data Seed (deployment script) ==='
Write-Host "WorkspaceName: $envWorkspaceName"
Write-Host "LakehouseName: $envLakehouseName"
Write-Host "RepositoryArchiveUrl: $envRepositoryArchiveUrl"

$workDir = Join-Path (Get-ScratchRoot) 'fabric-seed'
if (Test-Path -LiteralPath $workDir) {
    Remove-Item -LiteralPath $workDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $workDir | Out-Null

$archivePath = Join-Path $workDir 'repo.zip'
Write-Host "Downloading repository archive: $envRepositoryArchiveUrl"
$downloadHeaders = @{}
if (-not [string]::IsNullOrWhiteSpace($envGithubToken)) {
    $downloadHeaders['Authorization'] = "Bearer $envGithubToken"
}
Invoke-WebRequest -Uri $envRepositoryArchiveUrl -Headers $downloadHeaders -OutFile $archivePath -UseBasicParsing

Write-Host 'Extracting repository archive...'
Expand-ArchiveCrossPlatform -ArchivePath $archivePath -DestinationPath $workDir

$repoRoot = Get-ChildItem $workDir -Directory | Where-Object {
    (Test-Path (Join-Path $_.FullName 'infra/scripts') -PathType Container) -and
    (Test-Path (Join-Path $_.FullName 'dataset-seed')   -PathType Container)
} | Select-Object -First 1

if (-not $repoRoot) {
    if ((Test-Path (Join-Path $workDir 'infra/scripts') -PathType Container) -and
        (Test-Path (Join-Path $workDir 'dataset-seed')   -PathType Container)) {
        $repoRoot = Get-Item $workDir
    }
}

if (-not $repoRoot) {
    throw "Could not locate extracted repository root. Expected either a wrapper directory containing both 'infra/scripts' and 'dataset-seed', or those two directories at the top level of the archive."
}

$scriptsDirectory = Join-Path $repoRoot.FullName 'infra/scripts'

Write-Host 'Pre-resolving Fabric lakehouse from workspace name...'
try {
    $provisionResult = & (Join-Path $scriptsDirectory 'provision-fabric-lakehouse.ps1') `
        -WorkspaceName $envWorkspaceName `
        -LakehouseName $envLakehouseName
}
catch {
    throw "provision-fabric-lakehouse.ps1 threw: $($_.Exception.Message)`n$($_.ScriptStackTrace)"
}
if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
    throw "provision-fabric-lakehouse.ps1 failed with exit code $LASTEXITCODE"
}

$envWorkspaceId = [string]$provisionResult.WorkspaceId
$envLakehouseId = [string]$provisionResult.LakehouseId

if ([string]::IsNullOrWhiteSpace($envWorkspaceId) -or [string]::IsNullOrWhiteSpace($envLakehouseId)) {
    throw 'Pre-resolve did not return a valid workspaceId and lakehouseId.'
}

Write-Host "Resolved WorkspaceId: $envWorkspaceId"
Write-Host "Resolved LakehouseId: $envLakehouseId"

$datasetSeedPath = Join-Path $repoRoot.FullName 'dataset-seed'
Write-Host "Running seed-fabric-structured-files.ps1 (dataset: $datasetSeedPath)..."
& (Join-Path $scriptsDirectory 'seed-fabric-structured-files.ps1') `
    -WorkspaceId $envWorkspaceId `
    -LakehouseId $envLakehouseId `
    -DatasetSeedPath $datasetSeedPath
if ($LASTEXITCODE -ne 0) {
    throw "seed-fabric-structured-files.ps1 failed with exit code $LASTEXITCODE"
}

$DeploymentScriptOutputs = @{}
$DeploymentScriptOutputs['workspaceId'] = $envWorkspaceId
$DeploymentScriptOutputs['lakehouseId'] = $envLakehouseId
$DeploymentScriptOutputs['lakehouseName'] = $envLakehouseName

Write-Host 'Fabric data seed completed successfully.'
Write-Host "WorkspaceId: $envWorkspaceId"
Write-Host "LakehouseId: $envLakehouseId"
