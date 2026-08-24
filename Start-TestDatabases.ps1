<#
.SYNOPSIS
    Create-or-reuse local Docker containers for the MSSQL and Postgres test databases used by
    -p:DataProvider=SqlServer / -p:DataProvider=Postgres test runs.

.DESCRIPTION
    Implements the local Docker provisioning step described in Database-Providers-Testing-Plan.md, chapter 5.2:

      For each of the two providers (MSSQL, Postgres):
        1. docker inspect the well-known container name. If it does not exist, create it (docker run) with a
           fixed, hardcoded password (deliberate - see plan chapter 5.4, no secret store, not production data).
        2. If it exists but is not running, docker start it and reuse it.
        3. If it already exists and is running, do nothing - just report it.
        4. Poll (never a single fixed sleep) until the server inside the container is actually ready to accept
           connections - not just "container is running", the SQL engine itself needs time to initialize.

    The container names (tightwiki-test-mssql / tightwiki-test-postgres) and ports (14330 / 54329) are
    deliberately different from whatever the developer might use for manual, ad-hoc smoke testing, so this
    script never touches/collides with a hand-run container or a locally installed SQL Server.

    This script does NOT create the TightWikiTest / tightwiki_test application database itself, and does NOT
    apply any schema/seed data. That happens automatically the first time the app/test fixture connects
    (ITwDatabaseManager.InitializeSchema -> EF Core Migrate(), which creates the target database on demand),
    exactly like it already does against a fresh LocalDB/manual Postgres instance today. This script's only job
    is making sure the underlying SQL Server / PostgreSQL process is up and reachable.

    Safe to run repeatedly (idempotent create-or-reuse) - re-running it after the containers are already up just
    confirms readiness and exits, it never destroys or recreates an existing container.

.PARAMETER TimeoutSeconds
    How long to wait for each server to report ready before giving up. Defaults to 120s, which comfortably
    covers a cold container start (image already pulled) plus first-time SQL Server/Postgres initialization.

.EXAMPLE
    .\Start-TestDatabases.ps1
#>

[CmdletBinding()]
param(
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'

# --- Fixed configuration -------------------------------------------------------------------------------------
# Deliberately hardcoded (Database-Providers-Testing-Plan.md chapter 5.4): local, disposable test containers,
# not production or shared data - no secret store, no env file outside this script.

$MssqlContainerName = 'tightwiki-test-mssql'
$MssqlImage = 'mcr.microsoft.com/mssql/server:2022-latest'
$MssqlHostPort = 14330
$MssqlSaPassword = 'TightWiki_Test_2026!'
$MssqlTestDatabase = 'TightWikiTest'

$PostgresContainerName = 'tightwiki-test-postgres'
$PostgresImage = 'postgres:17'
$PostgresHostPort = 54329
$PostgresUser = 'tightwiki'
$PostgresPassword = 'TightWiki_Test_2026!'
$PostgresTestDatabase = 'tightwiki_test'

# --- Helpers ---------------------------------------------------------------------------------------------------

function Assert-DockerAvailable {
    try {
        docker version --format '{{.Server.Version}}' | Out-Null
    }
    catch {
        throw "Docker does not appear to be installed. Install Docker Desktop and re-run this script."
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is installed but the daemon is not reachable (is Docker Desktop running?). 'docker version' exited with code $LASTEXITCODE."
    }
}

function Get-ContainerStatus {
    param([Parameter(Mandatory)][string]$Name)

    $running = docker inspect --format '{{.State.Running}}' $Name 2>$null
    if ($LASTEXITCODE -ne 0) {
        return [PSCustomObject]@{ Exists = $false; Running = $false }
    }
    return [PSCustomObject]@{ Exists = $true; Running = ($running.Trim() -eq 'true') }
}

function Wait-ForContainerLogPattern {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $logs = docker logs $Name 2>&1
        if ($logs -match $Pattern) {
            return $true
        }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Wait-ForPostgresReady {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        docker exec $Name pg_isready | Out-Null
        if ($LASTEXITCODE -eq 0) {
            return $true
        }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Initialize-TestContainer {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$FriendlyName,
        [Parameter(Mandatory)][scriptblock]$RunCommand,
        [Parameter(Mandatory)][scriptblock]$WaitCommand
    )

    $status = Get-ContainerStatus -Name $Name

    if (-not $status.Exists) {
        Write-Host "==> [$FriendlyName] Container '$Name' not found - creating it" -ForegroundColor Cyan
        & $RunCommand
        if ($LASTEXITCODE -ne 0) {
            throw "docker run failed for '$Name' (exit code $LASTEXITCODE)"
        }
    }
    elseif (-not $status.Running) {
        Write-Host "==> [$FriendlyName] Container '$Name' exists but is stopped - starting it" -ForegroundColor Cyan
        docker start $Name | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "docker start failed for '$Name' (exit code $LASTEXITCODE)"
        }
    }
    else {
        Write-Host "==> [$FriendlyName] Container '$Name' is already running - reusing it" -ForegroundColor Green
    }

    Write-Host "==> [$FriendlyName] Waiting for the server to accept connections (up to ${TimeoutSeconds}s)..." -ForegroundColor Cyan
    $ready = & $WaitCommand
    if (-not $ready) {
        throw "[$FriendlyName] did not become ready within ${TimeoutSeconds}s - inspect with: docker logs $Name"
    }
    Write-Host "==> [$FriendlyName] Ready." -ForegroundColor Green
}

# --- Sanity checks -----------------------------------------------------------------------------------------

Assert-DockerAvailable

# --- MSSQL ------------------------------------------------------------------------------------------------

Initialize-TestContainer `
    -Name $MssqlContainerName `
    -FriendlyName 'MSSQL' `
    -RunCommand {
        docker run -d `
            --name $MssqlContainerName `
            -e "ACCEPT_EULA=Y" `
            -e "MSSQL_SA_PASSWORD=$MssqlSaPassword" `
            -e "MSSQL_PID=Developer" `
            -p "${MssqlHostPort}:1433" `
            $MssqlImage | Out-Null
    } `
    -WaitCommand {
        Wait-ForContainerLogPattern -Name $MssqlContainerName -Pattern 'SQL Server is now ready for client connections' -TimeoutSeconds $TimeoutSeconds
    }

# --- Postgres -----------------------------------------------------------------------------------------------

Initialize-TestContainer `
    -Name $PostgresContainerName `
    -FriendlyName 'Postgres' `
    -RunCommand {
        docker run -d `
            --name $PostgresContainerName `
            -e "POSTGRES_USER=$PostgresUser" `
            -e "POSTGRES_PASSWORD=$PostgresPassword" `
            -p "${PostgresHostPort}:5432" `
            $PostgresImage | Out-Null
    } `
    -WaitCommand {
        Wait-ForPostgresReady -Name $PostgresContainerName -TimeoutSeconds $TimeoutSeconds
    }

# --- Summary -------------------------------------------------------------------------------------------------

Write-Host ""
Write-Host "Both test database containers are up. Connection strings expected by the test projects" -ForegroundColor Green
Write-Host "(TightWiki.Test.Library\appsettings.Development.{SqlServer,Postgres}.json):" -ForegroundColor Green
Write-Host ""
Write-Host "  MSSQL    (container '$MssqlContainerName', host port $MssqlHostPort):" -ForegroundColor Yellow
Write-Host "    Server=localhost,$MssqlHostPort;Database=$MssqlTestDatabase;User Id=sa;Password=$MssqlSaPassword;TrustServerCertificate=True;"
Write-Host ""
Write-Host "  Postgres (container '$PostgresContainerName', host port $PostgresHostPort):" -ForegroundColor Yellow
Write-Host "    Host=localhost;Port=$PostgresHostPort;Database=$PostgresTestDatabase;User Id=$PostgresUser;Password=$PostgresPassword"
Write-Host ""
Write-Host "Neither database ($MssqlTestDatabase / $PostgresTestDatabase) is created by this script - the app/test" -ForegroundColor Yellow
Write-Host "fixture creates it on first connect (InitializeSchema -> EF Core Migrate())." -ForegroundColor Yellow
Write-Host ""
Write-Host "Containers are left running - re-run this script any time to check/restart them, or 'docker stop $MssqlContainerName $PostgresContainerName' to stop them yourself." -ForegroundColor Yellow
