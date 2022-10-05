IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetRelatedPages]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetRelatedPages] AS'
END
GO

ALTER PROCEDURE [dbo].[GetRelatedPages]
(
	@PageId int
) as
BEGIN--PROCEDURE
	SELECT TOP 100
		P.Id,
		P.[Name],
		P.[Revision],
		P.Navigation,
		P.[Description],
		Hits.Matches
	FROM
		[Page] as P
	INNER JOIN (
			SELECT
				RelatedPT.PageId,
				COUNT(0) as Matches
			FROM
				PageTag as RootPT
			INNER JOIN PageTag as RelatedPT
				ON RelatedPT.Tag = RootPT.Tag
			WHERE
				RootPT.PageId = @PageId
			GROUP BY
				RelatedPT.PageId
		) as Hits
		ON Hits.PageId = P.Id
END--PROCEDURE