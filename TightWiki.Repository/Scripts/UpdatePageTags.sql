BEGIN TRANSACTION;

DELETE FROM
	PageTag
WHERE
	PageId = @PageId;

INSERT INTO PageTag
(
	PageId,
	[Tag]
)
SELECT
	@PageId,
	T.[Value]
FROM
	TempTags as T
WHERE
	Coalesce(T.value, '') <> '';

COMMIT TRANSACTION;
