@echo off
set path=%PATH%;C:\Program Files\7-Zip;

REM -------------------Generate seed data. This also (re)writes ".\Seed\tightwiki.seed.zip" as a side effect
REM (GenerateSeedData\Program.cs) - the provider-neutral seed package the SqlServer/Postgres builds below need.
rd .\Publish /q /s
md Publish
dotnet publish .\GenerateSeedData -c Release -o publish\GenerateSeedData --runtime win-x64 --self-contained false
publish\GenerateSeedData\GenerateSeedData.exe ".\Data" ".\TightWiki.Repository\Defaults"

REM -------------------Build
rd .\Publish /q /s
md Publish
md Publish\TightWiki.Windows.x64\Deployment\data
md Publish\TightWiki.Linux.x64\Deployment\data

dotnet publish .\TightWiki -c Release -o publish\TightWiki.Windows.x64\Deployment\Site --runtime win-x64 --self-contained false
dotnet publish .\TightWiki.Plugin.Default -c Release -o publish\TightWiki.Windows.x64\Plugins --runtime win-x64 --self-contained false
md .\Publish\TightWiki.Windows.x64\Deployment\Site\Plugins\
Copy ".\Publish\TightWiki.Windows.x64\Plugins\TightWiki.Plugin.Default.dll" ".\Publish\TightWiki.Windows.x64\Deployment\Site\Plugins\"

dotnet publish .\TightWiki -c Release -o publish\TightWiki.Linux.x64\Deployment\Site --runtime linux-x64 --self-contained false
dotnet publish .\TightWiki.Plugin.Default -c Release -o publish\TightWiki.Linux.x64\Plugins --runtime linux-x64 --self-contained false
md .\Publish\TightWiki.Linux.x64\Deployment\Site\Plugins\
Copy ".\Publish\TightWiki.Linux.x64\Plugins\TightWiki.Plugin.Default.dll" ".\Publish\TightWiki.Linux.x64\Deployment\Site\Plugins\"

REM -------------------Make sure the defaults.db is not packaged.
del .\Data\defaults.db

copy .\Data\*.* Publish\TightWiki.Windows.x64\Deployment\data
copy .\Data\*.* Publish\TightWiki.Linux.x64\Deployment\data

7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Windows.x64.zip" ".\Publish\TightWiki.Windows.x64\Deployment\*.*"
7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Linux.x64.zip" ".\Publish\TightWiki.Linux.x64\Deployment\*.*"

REM -------------------EF Core builds (SqlServer/Postgres).
REM These do NOT get Data\*.* - that seed path is SQLite-only (see above). Instead they need the
REM provider-neutral seed package generated above (".\Seed\tightwiki.seed.zip") copied to
REM "Seed\tightwiki.seed.zip" next to the published app - the fixed, non-configurable location
REM EfDefaultsRepository.DefaultSeedPackagePath (TightWiki.Data.EfCore\Seeding\EfDefaultsRepository.cs) reads at
REM first run to seed a freshly created, otherwise-empty database. TightWiki.Plugin.Default.dll doesn't depend
REM on DataProvider, so the copies already published above (Windows.x64/Linux.x64 Plugins folders) are reused
REM instead of publishing it again for every provider.
if not exist ".\Seed\tightwiki.seed.zip" (
    echo ERROR: .\Seed\tightwiki.seed.zip is missing - SqlServer/Postgres builds need it to seed a brand-new database and will fail at first run without it. Skipping SqlServer/Postgres packaging.
    goto :SkipEfCoreBuilds
)

dotnet publish .\TightWiki -c Release -p:DataProvider=SqlServer -o publish\TightWiki.Windows.x64.SqlServer\Deployment\Site --runtime win-x64 --self-contained false
md .\Publish\TightWiki.Windows.x64.SqlServer\Deployment\Site\Plugins\
Copy ".\Publish\TightWiki.Windows.x64\Plugins\TightWiki.Plugin.Default.dll" ".\Publish\TightWiki.Windows.x64.SqlServer\Deployment\Site\Plugins\"
md .\Publish\TightWiki.Windows.x64.SqlServer\Deployment\Site\Seed\
Copy ".\Seed\tightwiki.seed.zip" ".\Publish\TightWiki.Windows.x64.SqlServer\Deployment\Site\Seed\"

dotnet publish .\TightWiki -c Release -p:DataProvider=SqlServer -o publish\TightWiki.Linux.x64.SqlServer\Deployment\Site --runtime linux-x64 --self-contained false
md .\Publish\TightWiki.Linux.x64.SqlServer\Deployment\Site\Plugins\
Copy ".\Publish\TightWiki.Linux.x64\Plugins\TightWiki.Plugin.Default.dll" ".\Publish\TightWiki.Linux.x64.SqlServer\Deployment\Site\Plugins\"
md .\Publish\TightWiki.Linux.x64.SqlServer\Deployment\Site\Seed\
Copy ".\Seed\tightwiki.seed.zip" ".\Publish\TightWiki.Linux.x64.SqlServer\Deployment\Site\Seed\"

dotnet publish .\TightWiki -c Release -p:DataProvider=Postgres -o publish\TightWiki.Windows.x64.Postgres\Deployment\Site --runtime win-x64 --self-contained false
md .\Publish\TightWiki.Windows.x64.Postgres\Deployment\Site\Plugins\
Copy ".\Publish\TightWiki.Windows.x64\Plugins\TightWiki.Plugin.Default.dll" ".\Publish\TightWiki.Windows.x64.Postgres\Deployment\Site\Plugins\"
md .\Publish\TightWiki.Windows.x64.Postgres\Deployment\Site\Seed\
Copy ".\Seed\tightwiki.seed.zip" ".\Publish\TightWiki.Windows.x64.Postgres\Deployment\Site\Seed\"

dotnet publish .\TightWiki -c Release -p:DataProvider=Postgres -o publish\TightWiki.Linux.x64.Postgres\Deployment\Site --runtime linux-x64 --self-contained false
md .\Publish\TightWiki.Linux.x64.Postgres\Deployment\Site\Plugins\
Copy ".\Publish\TightWiki.Linux.x64\Plugins\TightWiki.Plugin.Default.dll" ".\Publish\TightWiki.Linux.x64.Postgres\Deployment\Site\Plugins\"
md .\Publish\TightWiki.Linux.x64.Postgres\Deployment\Site\Seed\
Copy ".\Seed\tightwiki.seed.zip" ".\Publish\TightWiki.Linux.x64.Postgres\Deployment\Site\Seed\"

7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Windows.x64.SqlServer.zip" ".\Publish\TightWiki.Windows.x64.SqlServer\Deployment\*.*"
7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Linux.x64.SqlServer.zip" ".\Publish\TightWiki.Linux.x64.SqlServer\Deployment\*.*"
7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Windows.x64.Postgres.zip" ".\Publish\TightWiki.Windows.x64.Postgres\Deployment\*.*"
7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Linux.x64.Postgres.zip" ".\Publish\TightWiki.Linux.x64.Postgres\Deployment\*.*"

rd .\Publish\TightWiki.Windows.x64.SqlServer /q /s
rd .\Publish\TightWiki.Linux.x64.SqlServer /q /s
rd .\Publish\TightWiki.Windows.x64.Postgres /q /s
rd .\Publish\TightWiki.Linux.x64.Postgres /q /s

:SkipEfCoreBuilds

dotnet pack .\TightWiki.Plugin -c Release -o ".\Publish"

rd .\Publish\TightWiki.Windows.x64 /q /s
rd .\Publish\TightWiki.Linux.x64 /q /s

pause
