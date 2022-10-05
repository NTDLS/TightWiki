IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpdateUser]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpdateUser] AS'
END
GO

ALTER PROCEDURE [dbo].[UpdateUser]
(
	@Id as int,
	@EmailAddress as nvarchar (128),
	@AccountName as nvarchar (128),
	@Navigation as nvarchar (128) = NULL,
	@PasswordHash as nvarchar (128) = NULL,
	@FirstName as nvarchar (128) = NULL,
	@LastName as nvarchar (128) = NULL,
	@TimeZone as varchar (50) = NULL,
	@Country as nvarchar (100) = NULL,
	@AboutMe as nvarchar (MAX) = NULL,
	@ModifiedDate as datetime = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	UPDATE
		[User]
	SET
		[EmailAddress] = @EmailAddress,
		[AccountName] = @AccountName,
		[Navigation] = @Navigation,
		[PasswordHash] = @PasswordHash,
		[FirstName] = @FirstName,
		[LastName] = @LastName,
		[TimeZone] = @TimeZone,
		[Country] = @Country,
		[AboutMe] = @AboutMe
	FROM
		[User]
	WHERE
		Id = @Id

END--PROCEDURE