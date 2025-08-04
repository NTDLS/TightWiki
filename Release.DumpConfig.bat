@echo off
set path=%PATH%;C:\Program Files\7-Zip;

rd .\Publish /q /s

md Publish

dotnet publish .\DumpConfiguration -c Release -o publish\DumpConfiguration --runtime win-x64 --self-contained false

del .\TightWiki.Repository\Scripts\Initialization\Versions\999.999.999\*.* /q

publish\DumpConfiguration\DumpConfiguration.exe ".\Data" ".\TightWiki.Repository\Scripts\Initialization\Versions\999.999.999"

pause
