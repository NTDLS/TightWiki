SELECT
	P.Id as SourcePageId,
	P.[Name] as SourcePageName,
	P.[Navigation] as SourcePageNavigation,
	PR.ReferencesPageName as TargetPageName,
	PR.ReferencesPageNavigation as TargetPageNavigation,
	@PageSize as PaginationSize,
	(
		SELECT
			Count(0) / (@PageSize + 0.0)
		FROM
			PageReference as PR
		INNER JOIN [Page] as P
			ON P.Id = PR.PageId
		WHERE
			PR.ReferencesPageId IS NULL
	) as PaginationCount
FROM
	PageReference as PR
INNER JOIN [Page] as P
	ON P.Id = PR.PageId
WHERE
	PR.ReferencesPageId IS NULL
ORDER BY
	P.[Name],
	PR.PageId
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize