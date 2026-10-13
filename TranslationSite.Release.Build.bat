@echo off
set path=%PATH%;C:\Program Files\7-Zip;

REM -------------------Build
rd .\Publish /q /s
md Publish

dotnet publish .\TightWiki.TranslationSite -c Release -o publish\TightWiki.TranslationSite.Windows.x64\Deployment\Site --runtime win-x64 --self-contained false
7z.exe a -tzip -r -mx9 ".\Publish\TightWiki.TranslationSite.Windows.x64.zip" ".\publish\TightWiki.TranslationSite.Windows.x64\Deployment\*.*"
rd .\Publish\TightWiki.TranslationSite.Windows.x64 /q /s

pause
