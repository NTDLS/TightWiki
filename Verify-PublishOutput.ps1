<#
.SYNOPSIS
    Verifies that a Release publish of TightWiki/TightWiki.csproj for -p:DataProvider=SqlServer or =Postgres
    does not pull any SQLite/Dapper DLL into the publish output.

.DESCRIPTION
    Database-Providers-Testing-Plan.md chapter 3 ("Mechanicky guardrail") / chapter 7 (T6): TightWiki.csproj only
    references TightWiki.Repository (Dapper + NTDLS.SqliteDapperWrapper over SQLite) when
    '$(DataProvider)'=='Sqlite' - for SqlServer/Postgres it instead references the thin
    TightWiki.Data.EfCore.SqlServer / TightWiki.Data.EfCore.Postgres projects, which deliberately carry no
    SQLite/Dapper PackageReference and no ProjectReference to TightWiki.Repository (see the comments in those
    two .csproj files). This script turns that "should never happen" into something that is actually checked,
    instead of relying on a tester eyeballing a publish folder by hand after every phase (as documented in the
    project-database-providers memory).

    Forbidden DLL name patterns and why each one is here (verified against TightWiki.Repository.csproj's
    PackageReference list and the transitive dependency tree resolved into ~/.nuget/packages, not guessed):
      - Dapper*.dll                       - TightWiki.Repository's PackageReference NTDLS.SqliteDapperWrapper
                                             depends on Dapper directly.
      - NTDLS.SqliteDapperWrapper*.dll     - TightWiki.Repository's own PackageReference (the SQLite/Dapper glue
                                             used by every repository class).
      - Microsoft.Data.Sqlite*.dll         - transitive dependency of NTDLS.SqliteDapperWrapper, and also an
                                             explicit PackageReference of TightWiki.csproj itself, conditioned on
                                             '$(DataProvider)'=='Sqlite' (Microsoft.EntityFrameworkCore.Sqlite
                                             pulls in Microsoft.Data.Sqlite in turn).
      - SQLitePCLRaw*.dll                  - transitive dependency chain of Microsoft.Data.Sqlite
                                             (SQLitePCLRaw.core / .provider.e_sqlite3 / .batteries_v2, etc).
      - e_sqlite3.dll                      - the native SQLite engine binary shipped by the
                                             SQLitePCLRaw.lib.e_sqlite3 package (runtimes\<rid>\native\e_sqlite3.dll).
                                             It doesn't carry a "SQLitePCLRaw" prefix in its own filename, so it
                                             needs its own explicit pattern - this is the actual SQLite engine and
                                             the single strongest signal of a leak.
      - Microsoft.EntityFrameworkCore.Sqlite*.dll
                                            - TightWiki.csproj's own PackageReference
                                             Microsoft.EntityFrameworkCore.Sqlite, conditioned on
                                             '$(DataProvider)'=='Sqlite'. Distinct assembly from the
                                             provider-agnostic Microsoft.EntityFrameworkCore.dll (which legitimately
                                             ships in every build, SqlServer/Postgres included, and must NOT be
                                             flagged) - confirmed by actually publishing DataProvider=Sqlite and
                                             inspecting the output, not guessed.

    Usage:
      1. dotnet restore TightWiki\TightWiki.csproj -p:DataProvider=<X>   (explicit restore - switching
         -p:DataProvider= requires a fresh restore, an existing restore for a different provider's
         project.assets.json will not pick up the new conditional ProjectReference/PackageReference graph).
      2. dotnet publish TightWiki\TightWiki.csproj -c Release -p:DataProvider=<X> -o <temp dir> --no-restore
      3. Recursively scan the publish output (including a Plugins\ subfolder, if one happens to be present -
         TightWiki.Plugin.Default's DLL gets copied there by a separate build step, not by this publish) for the
         patterns above.
      4. Report and fail (exit 1) on any match; succeed (exit 0) otherwise.

    The temporary publish output goes under '.scratch\publish-verify-<DataProvider>' (gitignored) and is deleted
    again once the check completes, successfully or not - this script never leaves a permanent artifact in the
    repo.

.PARAMETER DataProvider
    'SqlServer' or 'Postgres'. 'Sqlite' is rejected with an error rather than silently no-op'd: a SQLite publish
    is *expected* to contain every DLL this script looks for, so running it against DataProvider=Sqlite would
    either always fail (useless noise) or have to special-case itself into doing nothing - and a silent no-op is
    exactly the kind of guardrail that quietly stops meaning anything if it's ever wired up for the wrong matrix
    value by mistake. Failing loudly makes a misconfigured CI step (or a typo'd local invocation) obvious
    immediately instead of masking it as a false "pass".

.EXAMPLE
    .\Verify-PublishOutput.ps1 -DataProvider SqlServer
.EXAMPLE
    .\Verify-PublishOutput.ps1 -DataProvider Postgres
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Sqlite', 'SqlServer', 'Postgres')]
    [string]$DataProvider
)

$ErrorActionPreference = 'Stop'

if ($DataProvider -eq 'Sqlite') {
    Write-Host "ERROR: -DataProvider Sqlite does not make sense for this check - the SQLite build is *supposed* to carry Microsoft.Data.Sqlite/SQLitePCLRaw/NTDLS.SqliteDapperWrapper/Dapper. This guardrail only exists to catch SqlServer/Postgres builds that leak SQLite. Pass -DataProvider SqlServer or -DataProvider Postgres." -ForegroundColor Red
    exit 1
}

# --- Config -----------------------------------------------------------------------------------------------------

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $RepoRoot 'TightWiki\TightWiki.csproj'
$PublishDir = Join-Path $RepoRoot ".scratch\publish-verify-$DataProvider"

# See the .DESCRIPTION block above for what each pattern is and why it's here.
$ForbiddenPatterns = @(
    'Dapper*.dll',
    'NTDLS.SqliteDapperWrapper*.dll',
    'Microsoft.Data.Sqlite*.dll',
    'SQLitePCLRaw*.dll',
    'e_sqlite3.dll',
    'Microsoft.EntityFrameworkCore.Sqlite*.dll'
)

if (-not (Test-Path $ProjectPath)) {
    throw "Could not find $ProjectPath - is this script still at the repo root?"
}

# Clean up any stale leftovers from a previous, interrupted run before we start.
if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

try {
    Write-Host "==> Restoring TightWiki for -p:DataProvider=$DataProvider ..." -ForegroundColor Cyan
    dotnet restore $ProjectPath -p:DataProvider=$DataProvider
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed (exit code $LASTEXITCODE)"
    }

    Write-Host "==> Publishing TightWiki (Release, -p:DataProvider=$DataProvider) to '$PublishDir' ..." -ForegroundColor Cyan
    dotnet publish $ProjectPath -c Release -p:DataProvider=$DataProvider -o $PublishDir --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed (exit code $LASTEXITCODE)"
    }

    Write-Host "==> Scanning publish output for forbidden SQLite/Dapper DLLs..." -ForegroundColor Cyan
    $found = @()
    foreach ($pattern in $ForbiddenPatterns) {
        $found += Get-ChildItem -Path $PublishDir -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue
    }
    $found = $found | Sort-Object FullName -Unique

    if ($found.Count -gt 0) {
        Write-Host ""
        Write-Host "FAIL: found $($found.Count) forbidden SQLite/Dapper DLL(s) in the -DataProvider=$DataProvider publish output:" -ForegroundColor Red
        foreach ($f in $found) {
            $relative = $f.FullName.Substring($PublishDir.Length).TrimStart('\', '/')
            Write-Host "  - $relative" -ForegroundColor Red
        }
        exit 1
    }

    Write-Host ""
    Write-Host "OK: no SQLite/Dapper DLLs found in the -DataProvider=$DataProvider publish output." -ForegroundColor Green
    exit 0
}
finally {
    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
