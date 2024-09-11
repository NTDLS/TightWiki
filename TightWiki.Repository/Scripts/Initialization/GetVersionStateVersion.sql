CREATE TABLE IF NOT EXISTS VersionState
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
    Name TEXT NOT NULL UNIQUE, 
    Value TEXT NOT NULL
);

SELECT Value FROM VersionState WHERE Name = 'Version';
