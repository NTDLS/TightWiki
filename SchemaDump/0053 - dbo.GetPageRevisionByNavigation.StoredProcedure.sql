IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageRevisionByNavigation]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageRevisionByNavigation] AS'
END
GO



ALTER PROCEDURE [dbo].[GetPageRevisionByNavigation]
(
	@Navigation nVarChar(128),
	@Revision int = NULL
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		P.Id,
		P.[Name],
		PR.[Description],
		PR.Body,
		PR.Revision,
		P.Revision as LatestRevision,
		P.Navigation,
		P.CreatedByUserId,
		P.CreatedDate,
		PR.ModifiedByUserId,
		PR.ModifiedDate
	FROM
		[Page] as P
	INNER JOIN [PageRevision] as PR
		ON PR.PageId = P.Id
	WHERE
		P.Navigation = @Navigation
		AND PR.Revision = IsNull(@Revision, P.Revision)

END--PROCEDURE