IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetUserByEmailAndPasswordHash]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetUserByEmailAndPasswordHash] AS'
END
GO





ALTER PROCEDURE [dbo].[GetUserByEmailAndPasswordHash]
(
	@EmailAddress nVarChar(128),
	@PasswordHash nVarChar(128)
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
		AboutMe,
		CreatedDate,
		ModifiedDate,
		LastLoginDate,
		VerificationCode,
		EmailVerified
	FROM
		[User]
	WHERE
		EmailAddress = @EmailAddress
		AND PasswordHash = @PasswordHash

END--PROCEDURE