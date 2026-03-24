@echo off

REM -------------------Build translator.
rd .\Publish /q /s
md Publish
dotnet publish .\LocalizerScan -c Release -o publish\LocalizerScan --runtime win-x64 --self-contained false
publish\LocalizerScan\LocalizerScan.exe ".\\" ".\TightWiki\Translations"

pause
