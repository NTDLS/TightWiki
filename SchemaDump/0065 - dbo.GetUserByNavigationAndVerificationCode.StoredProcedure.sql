IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetUserByNavigationAndVerificationCode]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetUserByNavigationAndVerificationCode] AS'
END
GO




ALTER PROCEDURE [dbo].[GetUserByNavigationAndVerificationCode]
(
	@Navigation nvarchar(128),
	@VerificationCode varchar(20)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		Id,
		EmailAddress,
		AccountName,
		Navigation,
		PasswordHash,
		FirstName,
		LastName,
		TimeZone,
		Country,
		[Language],
		AboutMe,
		CreatedDate,
		ModifiedDate,
		LastLoginDate,
		VerificationCode,
		EmailVerified
	FROM
		[User]
	WHERE
		Navigation = @Navigation
		AND VerificationCode = @VerificationCode

END--PROCEDURE