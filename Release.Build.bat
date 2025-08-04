@echo off
set path=%PATH%;C:\Program Files\7-Zip;

rd .\Publish /q /s

md Publish
md Publish\TightWiki.Windows.x64
md Publish\TightWiki.Windows.x64\data
md Publish\TightWiki.Linux.x64
md Publish\TightWiki.Linux.x64\data

dotnet publish .\TightWiki -c Release -o publish\TightWiki.Windows.x64\Site --runtime win-x64 --self-contained false
dotnet publish .\TightWiki -c Release -o publish\TightWiki.Linux.x64\Site --runtime linux-x64 --self-contained false

copy .\Data\*.* Publish\TightWiki.Windows.x64\data
copy .\Data\*.* Publish\TightWiki.Linux.x64\data

7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Windows.x64.zip" ".\Publish\TightWiki.Windows.x64\*.*"
7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.Linux.x64.zip" ".\Publish\TightWiki.Linux.x64\*.*"

rd .\Publish\TightWiki.Windows.x64 /q /s
rd .\Publish\TightWiki.Linux.x64 /q /s

pause
