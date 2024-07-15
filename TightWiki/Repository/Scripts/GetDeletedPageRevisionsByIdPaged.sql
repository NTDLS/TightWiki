SELECT
	PR.PageId as Id,
	PR.Name,
	PR.[Description],
	PR.Revision as Revision,
	DM.DeletedDate,
	DeletedUser.AccountName as DeletedByUserName,

	@PageSize as PaginationPageSize,
	(
		SELECT
			Count(0) / (@PageSize + 0.0)
		FROM
			[PageRevision] as PR
		WHERE
			PR.PageId = @PageId
	) as PaginationPageCount
FROM
	[PageRevision] as PR
INNER JOIN DeletionMeta as DM
	ON DM.PageId = PR.PageId
	AND DM.Revision = PR.Revision
INNER JOIN users_db.Profile as DeletedUser
	ON DeletedUser.UserId = DM.DeletedByUserID
WHERE
	PR.PageId = @PageId
ORDER BY
	PR.Revision
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
