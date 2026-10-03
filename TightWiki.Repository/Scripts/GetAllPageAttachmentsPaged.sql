WITH Attachments AS (
	SELECT
		PFR.PageFileId,
		PFR.Revision as FileRevision,
		PFR.Size,
		PFR.ContentType,
		PFR.CreatedDate,
		PF.Name as FileName,
		PF.Navigation as FileNavigation,
		P.Name as PageName,
		P.Namespace,
		P.Navigation as PageNavigation,
		(
			SELECT COUNT(0)
			FROM PageRevisionAttachment as PRA
			WHERE PRA.PageFileId = PFR.PageFileId
				AND PRA.FileRevision = PFR.Revision
		) as PageRevisionCount,
		(
			SELECT COUNT(0)
			FROM PageRevisionAttachment as PRA
			WHERE PRA.PageFileId = PFR.PageFileId
				AND PRA.FileRevision = PFR.Revision
				AND PRA.PageRevision = P.Revision
		) as CurrentPageRevisionCount
	FROM
		PageFileRevision as PFR
	INNER JOIN PageFile as PF
		ON PF.Id = PFR.PageFileId
	INNER JOIN Page as P
		ON P.Id = PF.PageId
)
SELECT
	A.*,
	@PageSize as PaginationPageSize,
	(
		SELECT
			(Count(0) + (@PageSize - 1)) / @PageSize
		FROM
			Attachments as C
		WHERE
			(@Status = 0
				OR (@Status = 1 AND C.PageRevisionCount = 0)
				OR (@Status = 2 AND C.PageRevisionCount > 0))
			AND (@Search = ''
				OR C.FileName LIKE '%' || @Search || '%'
				OR C.PageName LIKE '%' || @Search || '%'
				OR C.Namespace LIKE '%' || @Search || '%'
				OR C.ContentType LIKE '%' || @Search || '%')
	) as PaginationPageCount
FROM
	Attachments as A
WHERE
	(@Status = 0
		OR (@Status = 1 AND A.PageRevisionCount = 0)
		OR (@Status = 2 AND A.PageRevisionCount > 0))
	AND (@Search = ''
		OR A.FileName LIKE '%' || @Search || '%'
		OR A.PageName LIKE '%' || @Search || '%'
		OR A.Namespace LIKE '%' || @Search || '%'
		OR A.ContentType LIKE '%' || @Search || '%')
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Page=A.PageName
File=A.FileName
Type=A.ContentType
Size=A.Size
Revision=A.FileRevision
Created=A.CreatedDate
Usage=A.PageRevisionCount
*/
--::CONFIG
ORDER BY
	A.Size DESC
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
