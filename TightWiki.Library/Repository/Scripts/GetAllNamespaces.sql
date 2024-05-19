SELECT
	[Namespace],
	Count(0) as [CountOfPages]
FROM
	[Page]
GROUP BY
	[Namespace]
