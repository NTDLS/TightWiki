INSERT INTO [Emoji]
(
	[Name],
	ImageData,
	MimeType
)
SELECT
	@Name,
	@ImageData,
	@MimeType;

SELECT last_insert_rowid();
