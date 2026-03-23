INSERT INTO Log
(
	SeverityId,
	Text,
	ExceptionText,
	StackTrace,
	CreatedDate
)
SELECT
	S.Id,
	@Text,
	@ExceptionText,
	@StackTrace,
	@CreatedDate
FROM
	Severity as S
WHERE
	S.Name = @SeverityName