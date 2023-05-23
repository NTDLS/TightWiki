@Echo Off

SET PATH=%PATH%;C:\Program Files\Vroom Performance Technologies\SQL Script Generator;C:\Program Files\7-Zip;

Echo Creating new schema dump directory...
MD "TightWiki.Release.Database.Schema"

Echo Dumping database schema...
SSGC.exe /Config:_DumpSchemaConfig.json

Echo Deleting existing archive...
del "TightWiki.Release.Database.Schema.zip"

Echo Creatng new archive...
7z.exe a -tzip -mx9 "TightWiki.Release.Database.Schema.zip" "TightWiki.Release.Database.Schema\*.*"

RD "TightWiki.Release.Database.Schema" /Q /S
pause
