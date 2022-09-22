IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[DeleteUserById]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[DeleteUserById] AS'
END
GO


ALTER PROCEDURE [dbo].[DeleteUserById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	DELETE FROM
		[User]
	WHERE
		Id = @Id

END--PROCEDURE