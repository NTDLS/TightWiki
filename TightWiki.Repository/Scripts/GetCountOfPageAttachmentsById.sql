SELECT
	Count(0) as Attachments
FROM
	PageFile
WHERE
	PageId = @PageId
