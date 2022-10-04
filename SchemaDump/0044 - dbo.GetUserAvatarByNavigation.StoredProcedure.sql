IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetUserAvatarByNavigation]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetUserAvatarByNavigation] AS'
END
GO

ALTER PROCEDURE [dbo].[GetUserAvatarByNavigation]
(
	@Navigation nvarchar(128)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		[Avatar]
	FROM
		[User]
	WHERE
		Navigation = @Navigation

END--PROCEDURE