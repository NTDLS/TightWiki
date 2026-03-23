SELECT
	P.Id,
	P.[Name],
	P.[Revision],
	P.Navigation,
	P.[Description],
	@PageSize as PaginationPageSize,
	(
		SELECT
			(Count(0) + (@PageSize - 1)) / @PageSize
		FROM
			PageReference as PR
		INNER JOIN [Page] as P
			ON P.Id = PR.PageId
		WHERE
			PR.ReferencesPageId = @PageId
			AND PR.PageId <> PR.ReferencesPageId
	) as PaginationPageCount
FROM
	PageReference as PR
INNER JOIN [Page] as P
	ON P.Id = PR.PageId
WHERE
	PR.ReferencesPageId = @PageId
	AND PR.PageId <> PR.ReferencesPageId
ORDER BY
	P.[Name]
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
