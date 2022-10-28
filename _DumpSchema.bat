@Echo Off

MD SchemaDump

SET PATH=%PATH%;C:\Program Files\Vroom Performance Technologies\SQL Script Generator;

Echo Dumping database schema...
SSGC.exe /Config:_DumpSchemaConfig.json

Pause
