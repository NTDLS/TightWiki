IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetAllRoles]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetAllRoles] AS'
END
GO




ALTER PROCEDURE [dbo].[GetAllRoles] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		Id,
		[Name],
		[Description]
	FROM
		[Role]
	ORDER BY
		[Name]

END--PROCEDURE