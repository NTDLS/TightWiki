SELECT
	COUNT(0)
FROM
	Log as L
INNER JOIN Severity as S
	ON S.Id = L.SeverityId
WHERE
	S.Name = 'Error'
