IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpdateUserVerificationCode]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpdateUserVerificationCode] AS'
END
GO

ALTER PROCEDURE [dbo].[UpdateUserVerificationCode]
(
	@UserId as int,
	@VerificationCode varchar(20)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	UPDATE
		[User]
	SET
		VerificationCode = @VerificationCode
	WHERE
		Id = @UserId

END--PROCEDURE