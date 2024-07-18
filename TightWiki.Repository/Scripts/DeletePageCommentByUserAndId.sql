DELETE FROM
	PageComment
WHERE
	PageId = @PageId
	AND UserId = @UserId
	AND Id = @CommentId

