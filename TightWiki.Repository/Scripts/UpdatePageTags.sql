BEGIN TRANSACTION;

DELETE FROM
	PageTag
WHERE
	PageId = @PageId;

INSERT INTO PageTag
(
	PageId,
	[Tag],
	[Navigation]
)
SELECT
	@PageId,
	T.[Tag],
	T.[Navigation]
FROM
	TempTags as T
WHERE
	Coalesce(T.[Tag], '') <> '';

COMMIT TRANSACTION;
