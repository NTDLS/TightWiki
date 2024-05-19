INSERT INTO PageComment
(
	PageId,
	CreatedDate,
	UserId,
	Body
)
SELECT
	@PageId,
	@CreatedDate,
	@UserId,
	@Body
