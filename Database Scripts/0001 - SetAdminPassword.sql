--DECLARE @PlainTextPassword VARCHAR(128) = '2TightWiki' --You can set the admin password here.
DECLARE @PlainTextPassword VARCHAR(128) = @@SERVERNAME
UPDATE [User] SET PasswordHash = LOWER(Convert(VARCHAR(128), HASHBYTES('SHA2_256', @PlainTextPassword), 2)) WHERE AccountName = 'admin'
