DELETE FROM
	PageComment
WHERE
	PageId = @PageId
	AND Id = @CommentId

