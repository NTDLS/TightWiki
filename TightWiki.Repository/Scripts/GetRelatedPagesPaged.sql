SELECT
    P.Id,
    P.[Name],
    P.[Revision],
    P.Navigation,
    P.[Description],
	@PageSize as PaginationPageSize,
	(COUNT(*) OVER() + (@PageSize - 1)) / @PageSize AS PaginationPageCount
FROM
    (
        --Backlinks.
        SELECT DISTINCT
            P.Id,
            P.[Name],
            P.[Revision],
            P.Navigation,
            P.[Description]
        FROM
            PageReference AS PR
        INNER JOIN [Page] AS P
            ON P.Id = PR.PageId
        WHERE
            PR.ReferencesPageId = @PageId
            AND PR.PageId <> @PageId

        UNION

        --Outlinks.
        SELECT DISTINCT
            P.Id,
            P.[Name],
            P.[Revision],
            P.Navigation,
            P.[Description]
        FROM
            PageReference AS PR
        INNER JOIN [Page] AS P
            ON P.Id = PR.ReferencesPageId
        WHERE
            PR.PageId = @PageId
            AND PR.ReferencesPageId <> @PageId

        UNION

        --Second order links.
        SELECT DISTINCT
            P.Id,
            P.[Name],
            P.[Revision],
            P.Navigation,
            P.[Description]
        FROM
            PageReference AS OutLinks
        INNER JOIN PageReference AS SecondOrder
            ON SecondOrder.ReferencesPageId = OutLinks.ReferencesPageId
        INNER JOIN [Page] AS P
            ON P.Id = SecondOrder.PageId
        WHERE
            OutLinks.PageId = @PageId
            AND P.Id <> @PageId
    ) AS Combined
INNER JOIN [Page] AS P
    ON P.Id = Combined.Id
ORDER BY
    Combined.[Name]
LIMIT @PageSize
OFFSET ((@PageNumber - 1) * @PageSize)
