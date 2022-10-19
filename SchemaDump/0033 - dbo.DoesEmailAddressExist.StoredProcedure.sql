IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[DoesEmailAddressExist]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[DoesEmailAddressExist] AS'
END
GO




ALTER PROCEDURE [dbo].[DoesEmailAddressExist]
(
	@EmailAddress nvarchar(128)
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT TOP 1
		1
	FROM
		[User]
	WHERE
		[EMailAddress] = @EmailAddress

END--PROCEDURE