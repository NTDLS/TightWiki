IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpdateUserAvatar]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpdateUserAvatar] AS'
END
GO

ALTER PROCEDURE [dbo].[UpdateUserAvatar]
(
	@UserId int,
	@Avatar varbinary(max)

) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	UPDATE
		[User]
	SET
		Avatar = @Avatar
	WHERE
		Id = @UserId

END--PROCEDURE