IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[DoesAccountNameExist]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[DoesAccountNameExist] AS'
END
GO




ALTER PROCEDURE [dbo].[DoesAccountNameExist]
(
	@AccountName nvarchar(128)
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT TOP 1
		1
	FROM
		[User]
	WHERE
		AccountName = @AccountName

END--PROCEDURE