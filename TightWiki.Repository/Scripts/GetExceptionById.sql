SELECT
	Id,
	[Text],
	[ExceptionText],
	[StackTrace],
	[CreatedDate]
FROM
	[Exception]
WHERE
	Id = @Id
