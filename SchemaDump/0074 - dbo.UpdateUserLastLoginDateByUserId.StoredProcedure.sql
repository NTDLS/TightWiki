IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpdateUserLastLoginDateByUserId]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpdateUserLastLoginDateByUserId] AS'
END
GO



ALTER PROCEDURE [dbo].[UpdateUserLastLoginDateByUserId]
(
	@UserId as int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	UPDATE
		[User]
	SET
		[LastLoginDate] = GETUTCDATE()
	FROM
		[User]
	WHERE
		[Id] = @UserId

END--PROCEDURE