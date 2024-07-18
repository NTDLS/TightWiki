BEGIN TRANSACTION;

DELETE FROM
	[PageToken]
WHERE
	[PageId] IN (SELECT PageId FROM TempTokens);

INSERT INTO [PageToken]
(
	[PageId],
	[Token],
	[DoubleMetaphone],
	[Weight]
)
SELECT
	[PageId],
	[Token],
	[DoubleMetaphone],
	[Weight]
FROM
	TempTokens;

COMMIT TRANSACTION;
