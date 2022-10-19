IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageRevisionById]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageRevisionById] AS'
END
GO




ALTER PROCEDURE [dbo].[GetPageRevisionById]
(
	@PageId Int,
	@Revision int = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		P.Id,
		P.[Name],
		P.[Description],
		PR.Body,
		PR.Revision,
		P.Navigation,
		P.CreatedByUserId,
		P.CreatedDate,
		P.ModifiedByUserId,
		P.ModifiedDate
	FROM
		[Page] as P
	INNER JOIN [PageRevision] as PR
		ON PR.PageId = P.Id
	WHERE
		P.Id = @PageId
		AND PR.Revision = IsNull(@Revision, P.Revision)

END--PROCEDURE