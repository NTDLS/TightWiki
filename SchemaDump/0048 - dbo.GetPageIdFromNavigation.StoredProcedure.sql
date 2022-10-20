IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageIdFromNavigation]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageIdFromNavigation] AS'
END
GO




ALTER PROCEDURE [dbo].[GetPageIdFromNavigation]
(
	@Navigation nVarChar(128)
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		P.Id
	FROM
		[Page] as P
	WHERE
		P.Navigation = @Navigation

END--PROCEDURE