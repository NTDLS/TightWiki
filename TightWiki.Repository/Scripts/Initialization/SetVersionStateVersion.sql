INSERT INTO VersionState (Name, Value)
VALUES ('Version', @Version)
ON CONFLICT(Name) DO UPDATE SET Value = @Version;
