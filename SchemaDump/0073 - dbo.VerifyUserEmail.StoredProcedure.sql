IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[VerifyUserEmail]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[VerifyUserEmail] AS'
END
GO

ALTER PROCEDURE [dbo].[VerifyUserEmail]
(
	@UserId as int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	UPDATE
		[User]
	SET
		EmailVerified = 1,
		VerificationCode = NULL
	WHERE
		Id = @UserId

END--PROCEDURE