IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpdateUserPassword]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpdateUserPassword] AS'
END
GO

ALTER PROCEDURE [dbo].[UpdateUserPassword]
(
	@UserId as int,
	@PasswordHash as nvarchar (128) 
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	UPDATE
		[User]
	SET
		[PasswordHash] = @PasswordHash
	FROM
		[User]
	WHERE
		Id = @UserId

END--PROCEDURE