@echo off

set path=%PATH%;C:\Program Files\7-Zip;

rd .\Publish /q /s

md Publish

dotnet publish .\GenerateSeedData -c Release -o publish\GenerateSeedData --runtime win-x64 --self-contained false

del .\TightWiki.Repository\Scripts\Initialization\Versions\999.999.999\*.sql /q

publish\GenerateSeedData\GenerateSeedData.exe ".\Data" ".\TightWiki.Repository\Scripts\Initialization\Versions\999.999.999"

chcp 65001 >nul
set ESC=
echo %ESC%[91m╔═══════════════════════════════[ REMINDER ]═══════════════════════════╗
echo %ESC%[91m║ MAKE SURE THESE FILES ARE SET AS "EMBEDDED RESOURCE" IN THE PROJECT! ║
echo %ESC%[91m╚══════════════════════════════════════════════════════════════════════╝
echo %ESC%[0m

pause
