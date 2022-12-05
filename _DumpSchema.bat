@Echo Off

SET PATH=%PATH%;C:\Program Files\Vroom Performance Technologies\SQL Script Generator;C:\Program Files\7-Zip;

Echo Creating new schema dump directory...
MD "Database Scripts"

Echo Dumping database schema...
SSGC.exe /Config:_DumpSchemaConfig.json

Echo Deleting existing archive...
del "TightWiki Database Scripts.zip"

Echo Creatng new archive...
7z.exe a -tzip -mx9 "TightWiki Database Scripts.zip" "Database Scripts\*.*"

Pause
