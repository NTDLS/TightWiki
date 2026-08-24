<#
.SYNOPSIS
    Regenerates the reference EF Core scaffold of the current SQLite schema (TightWiki.Data.EfCore\_Scaffold\).

.DESCRIPTION
    Implements steps 1-2 of the "Migrace schematu" process described in Database-Providers-Plan.md, chapter 5:

      1. Copies the 8 live SQLite database files from Data\ (everything except defaults.db, which is just a
         seed template, not production schema - see plan chapter 2.1a) into a temporary directory, then applies
         any pending versioned upgrade scripts to that COPY (never to the files under Data\) so the schema is
         at the version the current assembly expects. This is done via the throwaway _tools\SchemaUpgradeTool
         console app, which just constructs TightWiki.Repository.Helpers.DatabaseManager against the temp
         directory and calls ITwDatabaseManager.InitializeSchema() - the same upgrade path TightWiki\Program.cs
         runs at startup.
      2. Runs "dotnet ef dbcontext scaffold" once per database file against that temp copy, writing the
         generated entities + DbContext for each of the 8 schemas into TightWiki.Data.EfCore\_Scaffold\<Schema>\.
         Scaffolding is hosted by the standalone _tools\EfScaffoldTool project, which is the only place that
         references Microsoft.EntityFrameworkCore.Sqlite/.Design - TightWiki.Data.EfCore itself must stay free
         of any SQLite package (see plan chapters 4.2.1 and 9) and excludes _Scaffold\ from compilation.

    Step 3 (manual merge of the scaffold diff into the hand-maintained model in TightWiki.Data.EfCore), step 4
    ("dotnet ef migrations add" for the driver projects) and step 5 (the manual review checklist) are NOT part
    of this script yet - they require the driver projects from phase 2, which do not exist yet. This script
    only performs the automatable steps 1-2 and then stops.

.PARAMETER KeepTempCopy
    Leave the temporary directory (copied + upgraded .db files) behind for inspection instead of deleting it
    after the scaffold step completes. Useful for debugging a scaffold run.

.EXAMPLE
    .\Generate-EfMigrations.ps1
#>

[CmdletBinding()]
param(
    [switch]$KeepTempCopy
)

$ErrorActionPreference = 'Stop'

$RepoRoot = $PSScriptRoot
$DataDir = Join-Path $RepoRoot 'Data'
$SchemaUpgradeToolProject = Join-Path $RepoRoot '_tools\SchemaUpgradeTool\SchemaUpgradeTool.csproj'
$EfScaffoldToolProject = Join-Path $RepoRoot '_tools\EfScaffoldTool\EfScaffoldTool.csproj'
$ScaffoldRoot = Join-Path $RepoRoot 'TightWiki.Data.EfCore\_Scaffold'

# Maps each of the 8 live SQLite database files (Data\*.db, excluding defaults.db - see plan chapter 2.1a) to
# the schema name used both by DatabaseManager.cs's "Databases" list and by plan chapter 4.3.
$Databases = [ordered]@{
    'config.db'               = 'Config'
    'pages.db'                = 'Pages'
    'users.db'                = 'Users'
    'statistics.db'           = 'Statistics'
    'emoji.db'                = 'Emoji'
    'logging.db'              = 'Logging'
    'deletedpages.db'         = 'DeletedPages'
    'deletedpagerevisions.db' = 'DeletedPageRevisions'
}

function Invoke-Checked {
    param(
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][scriptblock]$Command
    )

    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Step failed (exit code $LASTEXITCODE): $Description"
    }
}

# --- Sanity checks -----------------------------------------------------------------------------------------

if (-not (Test-Path $DataDir)) {
    throw "Data directory not found: $DataDir"
}

try {
    & dotnet ef --version | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet-ef exited with code $LASTEXITCODE" }
}
catch {
    throw "The 'dotnet-ef' global tool is required but is not available. Install it with: dotnet tool install --global dotnet-ef"
}

# --- Step 1: copy Data\*.db (except defaults.db) to a temp directory and upgrade the copy -------------------

$TempDir = Join-Path ([System.IO.Path]::GetTempPath()) 'TightWikiEfScaffold'

if (Test-Path $TempDir) {
    Remove-Item -Path $TempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $TempDir | Out-Null

Write-Host "==> Copying Data\*.db (except defaults.db) to temporary directory: $TempDir" -ForegroundColor Cyan
foreach ($fileName in $Databases.Keys) {
    $source = Join-Path $DataDir $fileName
    if (-not (Test-Path $source)) {
        throw "Expected database file not found: $source"
    }
    Copy-Item -Path $source -Destination (Join-Path $TempDir $fileName) -Force
}

Invoke-Checked -Description "Building SchemaUpgradeTool" -Command {
    dotnet build $SchemaUpgradeToolProject --nologo -v minimal
}

Invoke-Checked -Description "Applying pending schema upgrade scripts to the temporary copy" -Command {
    dotnet run --no-build --project $SchemaUpgradeToolProject -- $TempDir
}

# --- Step 2: scaffold each of the 8 databases into TightWiki.Data.EfCore\_Scaffold\<Schema>\ -----------------

if (Test-Path $ScaffoldRoot) {
    # Clean slate on every run so that tables/entities removed or renamed since the previous scaffold don't
    # linger as stale files - dotnet ef's --force only overwrites files it regenerates, it does not delete
    # files that no longer correspond to a table.
    Remove-Item -Path $ScaffoldRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $ScaffoldRoot | Out-Null

Invoke-Checked -Description "Building EfScaffoldTool" -Command {
    dotnet build $EfScaffoldToolProject --nologo -v minimal
}

foreach ($fileName in $Databases.Keys) {
    $schema = $Databases[$fileName]
    $dbPath = Join-Path $TempDir $fileName
    $outputDir = Join-Path $ScaffoldRoot $schema
    $namespace = "TightWiki.Data.EfCore._Scaffold.$schema"

    Invoke-Checked -Description "Scaffolding schema '$schema' from $fileName" -Command {
        dotnet ef dbcontext scaffold `
            "Data Source=$dbPath" `
            Microsoft.EntityFrameworkCore.Sqlite `
            --project $EfScaffoldToolProject `
            --no-build `
            -o $outputDir `
            --context "${schema}Context" `
            --namespace $namespace `
            --context-namespace $namespace `
            --no-onconfiguring `
            --force
    }
}

# --- Cleanup -------------------------------------------------------------------------------------------------

if ($KeepTempCopy) {
    Write-Host "==> Leaving temporary database copy in place: $TempDir" -ForegroundColor Yellow
}
else {
    Remove-Item -Path $TempDir -Recurse -Force
}

Write-Host ""
Write-Host "Scaffold complete. Generated entities + DbContext for all $($Databases.Count) schemas under:" -ForegroundColor Green
Write-Host "  $ScaffoldRoot"
Write-Host ""
Write-Host "Steps 1-2 only (copy + upgrade + scaffold) - this is where the automated part of the process ends" -ForegroundColor Yellow
Write-Host "(see Database-Providers-Plan.md, chapter 5):" -ForegroundColor Yellow
Write-Host "  Step 3 - diff this scaffold against the previous one (if this is not the first run) and manually" -ForegroundColor Yellow
Write-Host "           merge the relevant changes into the hand-maintained model in TightWiki.Data.EfCore." -ForegroundColor Yellow
Write-Host "  Step 4 - once driver projects exist (phase 2+), run 'dotnet ef migrations add <version>' in each." -ForegroundColor Yellow
Write-Host "  Step 5 - manually double check: collations (COLLATE NOCASE), VARCHAR lengths/numeric precision," -ForegroundColor Yellow
Write-Host "           NOT NULL correctness, indexes/unique constraints, and correct schema assignment." -ForegroundColor Yellow
