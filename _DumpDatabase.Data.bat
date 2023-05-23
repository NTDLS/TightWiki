@Echo Off

SET PATH=%PATH%;C:\Program Files\Vroom Performance Technologies\SQL Script Generator;C:\Program Files\7-Zip;

Echo Creating new schema dump directory...
MD "TightWiki.Release.Database.Data"

CreateDatabaseSeeds\bin\Debug\net7.0-windows\CreateDatabaseSeeds.exe "@SchemaExtractionScripts" "TightWiki.Release.Database.Data"

Echo Deleting existing archive...
del "TightWiki.Release.Database.Data.zip"

Echo Creatng new archive...
7z.exe a -tzip -mx9 "TightWiki.Release.Database.Data.zip" "TightWiki.Release.Database.Data\*.*"

RD "TightWiki.Release.Database.Data" /Q /S

Pause
