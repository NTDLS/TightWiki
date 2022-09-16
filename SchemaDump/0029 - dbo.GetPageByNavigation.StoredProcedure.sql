IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageByNavigation]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageByNavigation] AS'
END
GO

ALTER PROCEDURE [dbo].[GetPageByNavigation]
(
	@Navigation nVarChar(128)
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		P.Id,
		P.[Name],
		P.[Description],
		P.Body,
		P.Navigation,
		P.CreatedByUserId,
		P.CreatedDate,
		P.ModifiedByUserId,
		P.ModifiedDate
	FROM
		[Page] as P
	WHERE
		P.Navigation = @Navigation

END--PROCEDURE