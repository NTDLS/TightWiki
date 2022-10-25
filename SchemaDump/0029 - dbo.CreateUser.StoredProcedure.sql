IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[CreateUser]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[CreateUser] AS'
END
GO





ALTER PROCEDURE [dbo].[CreateUser]
(
	@EmailAddress as nvarchar (128),
	@AccountName as nvarchar (128),
	@Navigation as nvarchar (128) = NULL,
	@PasswordHash as nvarchar (128) = NULL,
	@FirstName as nvarchar (128) = NULL,
	@LastName as nvarchar (128) = NULL,
	@TimeZone as nvarchar (128) = NULL,
	@Language as nvarchar (128) = NULL,
	@Country as nvarchar (128) = NULL,
	@VerificationCode varchar(20)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	INSERT INTO [User]
	(
		[EmailAddress],
		[AccountName],
		[Navigation],
		[PasswordHash],
		[FirstName],
		[LastName],
		[TimeZone],
		[Country],
		[Language],
		[CreatedDate],
		[ModifiedDate],
		[LastLoginDate],
		[VerificationCode]
	)
	SELECT
		@EmailAddress,
		@AccountName,
		@Navigation,
		@PasswordHash,
		@FirstName,
		@LastName,
		@TimeZone,
		@Country,
		@Language,
		GETUTCDATE(),
		GETUTCDATE(),
		GETUTCDATE(),
		@VerificationCode

	SELECT cast(SCOPE_IDENTITY() as int) as UserId

END--PROCEDURE