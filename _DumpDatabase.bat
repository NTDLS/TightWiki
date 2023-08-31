@Echo Off

SET PATH=%PATH%;C:\Program Files\Vroom Performance Technologies\SQL Script Generator;C:\Program Files\7-Zip;

Echo Creating new schema dump directory...
MD "Database Setup Scripts"

Echo Dumping database schema...
SSGC.exe /Config:_DumpSchemaConfig.json

Echo Creating new schema dump directory...
MD "Database Setup Scripts\Data"

CreateDatabaseSeeds\bin\Debug\net7.0-windows\CreateDatabaseSeeds.exe "@SchemaExtractionScripts" "Database Setup Scripts\Data"

Echo Deleting existing archive...
del "TightWiki.Release.Database.zip"

Echo Creatng new archive...
7z.exe a -tzip -mx9 -r "TightWiki.Release.Database.zip" "Database Setup Scripts\*.*"

RD "Database Setup Scripts" /Q /S
pause
