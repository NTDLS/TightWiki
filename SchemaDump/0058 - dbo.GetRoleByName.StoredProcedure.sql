IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetRoleByName]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetRoleByName] AS'
END
GO

ALTER PROCEDURE [dbo].[GetRoleByName]
(
	@Name nVarChar(128)
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		Id,
		[Name],
		[Description]
	FROM
		[Role]
	WHERE
		[Name] = @Name

END--PROCEDURE