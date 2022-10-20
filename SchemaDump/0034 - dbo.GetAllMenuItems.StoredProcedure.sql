IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetAllMenuItems]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetAllMenuItems] AS'
END
GO




ALTER PROCEDURE [dbo].[GetAllMenuItems] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		[Id] as [Id],
		[Name] as [Name],
		[Link] as [Link],
		[Ordinal] as [Ordinal]
	FROM
		[MenuItem]
	ORDER BY
		[Ordinal]

END--PROCEDURE