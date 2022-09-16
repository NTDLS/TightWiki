IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetTopRecentlyModifiedPages]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetTopRecentlyModifiedPages] AS'
END
GO

ALTER PROCEDURE [dbo].[GetTopRecentlyModifiedPages]
(
	@TopCount int
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT TOP(@TopCount)
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
	ORDER BY
		P.ModifiedDate DESC,
		P.[Name] ASC

END--PROCEDURE