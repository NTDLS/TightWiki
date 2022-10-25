IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetUserByNavigation]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetUserByNavigation] AS'
END
GO





ALTER PROCEDURE [dbo].[GetUserByNavigation]
(
	@Navigation nvarchar(128)
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

END--PROCEDURE