SELECT
	P.Id as SourcePageId,
	P.[Name] as SourcePageName,
	P.[Navigation] as SourcePageNavigation,
	PR.ReferencesPageName as TargetPageName,
	PR.ReferencesPageNavigation as TargetPageNavigation,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			PageReference as PR
		INNER JOIN [Page] as P
			ON P.Id = PR.PageId
		WHERE
			PR.ReferencesPageId IS NULL
	) as PaginationPageCount
FROM
	PageReference as PR
INNER JOIN [Page] as P
	ON P.Id = PR.PageId
WHERE
	PR.ReferencesPageId IS NULL
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
SourcePage=P.[Name]
TargetPage=PR.ReferencesPageName
*/
--::CONFIG
ORDER BY
	P.[Name]
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
