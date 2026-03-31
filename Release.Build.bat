@echo off
set path=%PATH%;C:\Program Files\7-Zip;

REM -------------------Generate seed data.
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
dotnet publish .\TightWiki.Plugin.Default -c Release -o publish\TightWiki.Linux.x64\Plugins --runtime win-x64 --self-contained false
md .\Publish\TightWiki.Linux.x64\Deployment\Site\Plugins\
Copy ".\Publish\TightWiki.Linux.x64\Plugins\TightWiki.Plugin.Default.dll" ".\Publish\TightWiki.Linux.x64\Deployment\Site\Plugins\"

REM -------------------Make sure the defaults.db is not packaged.
del .\Data\defaults.db

copy .\Data\*.* Publish\TightWiki.Windows.x64\Deployment\data
copy .\Data\*.* Publish\TightWiki.Linux.x64\Deployment\data

7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Windows.x64.zip" ".\Publish\TightWiki.Windows.x64\Deployment\*.*"
7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Linux.x64.zip" ".\Publish\TightWiki.Linux.x64\Deployment\*.*"

dotnet pack .\TightWiki.Plugin -c Release -o ".\Publish"

rd .\Publish\TightWiki.Windows.x64 /q /s
rd .\Publish\TightWiki.Linux.x64 /q /s

pause
