SELECT
	P.Id,
	P.Name,
	P.Namespace,
	P.Navigation,
	P.Description,
	P.CreatedDate,
	P.ModifiedDate,
	PR.ModifiedDate as RevisionModifiedDate,
	PR.Revision,
	PR.DataHash,
	PR.Body
FROM
	Page as P
INNER JOIN PageRevision as PR
	ON P.Id = PR.PageId
INNER JOIN (
		SELECT
			P.Id as PageId,
			MAX(PR.Revision) as Revision
		FROM
			Page as P
		INNER JOIN PageRevision as PR
			ON P.Id = PR.PageId
		GROUP BY
			P.Id
	) as MaxRevisions
	ON MaxRevisions.PageId = PR.PageId
	AND MaxRevisions.Revision = PR.Revision;
