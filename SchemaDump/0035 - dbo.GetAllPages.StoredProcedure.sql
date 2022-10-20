IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetAllPages]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetAllPages] AS'
END
GO


ALTER PROCEDURE [dbo].[GetAllPages] AS
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
		PR.Revision = P.Revision

END--PROCEDURE