INSERT INTO [Exception]
(
	[Text],
	[ExceptionText],
	[StackTrace],
	[CreatedDate]
)
SELECT
	@Text,
	@ExceptionText,
	@StackTrace,
	@CreatedDate
