UPDATE
	[Emoji]
SET
	[Name] = @Name,
	ImageData = Coalesce(@ImageData, ImageData),
	MimeType = @MimeType
WHERE
	Id = @EmojiId
