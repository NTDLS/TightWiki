@echo off

rd .\Publish /q /s
md Publish
dotnet publish .\GenerateSeedData -c Release -o publish\GenerateSeedData --runtime win-x64 --self-contained false
publish\GenerateSeedData\GenerateSeedData.exe ".\Data" ".\TightWiki.Repository\Defaults"

pause
