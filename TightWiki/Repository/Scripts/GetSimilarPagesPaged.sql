SELECT
	P.Id,
	P.[Name],
	P.Navigation,
	P.[Description],
	P.Revision,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	P.ModifiedDate,
	@PageSize as PaginationPageSize,
	(
		SELECT
			Round(Count(0) / (@PageSize + 0.0)  + 0.999)
		FROM
			Page as P
		WHERE
			P.Id IN
			(
				SELECT
					PT.PageId
				FROM
					PageTag as RootTags
				LEFT OUTER JOIN PageTag as PT
					ON PT.Tag = RootTags.Tag
				WHERE
					RootTags.PageId = @PageId
				GROUP BY
					PT.PageId
				HAVING
					((Count(0) / (SELECT COUNT(0) FROM PageTag as iP WHERE iP.PageId = @PageId)) * 100.0) >= @Similarity
			)
	) as PaginationPageCount
FROM
	Page as P
WHERE
	P.Id IN
	(
		SELECT
			PT.PageId
		FROM
			PageTag as RootTags
		LEFT OUTER JOIN PageTag as PT
			ON PT.Tag = RootTags.Tag
		WHERE
			RootTags.PageId = @PageId
		GROUP BY
			PT.PageId
		HAVING
			((Count(0) / (SELECT COUNT(0) FROM PageTag as iP WHERE iP.PageId = @PageId)) * 100.0) >= @Similarity
	)
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
