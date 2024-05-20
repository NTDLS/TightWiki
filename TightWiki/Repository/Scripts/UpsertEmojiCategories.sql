DELETE FROM EmojiCategory WHERE Id IN
(
	SELECT
		EC.Id
	FROM
		EmojiCategory as EC
	LEFT OUTER JOIN TempEmojiCategories as TC
		ON TC.Value = EC.Category
	WHERE
		EC.EmojiId = 1
		AND TC.Value IS NULL
);

--Insert previously non-existing categories.
INSERT INTO EmojiCategory
(
	EmojiId,
	Category
)
SELECT
	@EmojiId,
	TC.Value
FROM
	TempEmojiCategories as TC
LEFT OUTER JOIN EmojiCategory as EC
	ON EC.Category = TC.Value
	AND EC.EmojiId = @EmojiId
WHERE
	EC.Id IS NULL;
