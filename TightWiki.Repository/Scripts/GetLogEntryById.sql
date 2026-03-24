SELECT
	L.Id,
	S.Name as Severity,
	L.[Text],
	L.[ExceptionText],
	L.[StackTrace],
	L.[CreatedDate]
FROM
	Log as L
INNER JOIN Severity as S
	ON S.Id = L.SeverityId
WHERE
	L.Id = @Id
