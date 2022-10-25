IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetUserById]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetUserById] AS'
END
GO




ALTER PROCEDURE [dbo].[GetUserById]
(
	@Id int
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
		[Language],
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
		Id = @Id

END--PROCEDURE