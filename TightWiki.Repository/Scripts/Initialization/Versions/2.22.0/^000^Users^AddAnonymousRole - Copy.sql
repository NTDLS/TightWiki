INSERT INTO Role
(
	Name,
	Description
)
SELECT
	'Anonymous',
	'Security group assigned to users that are not logged in.'
WHERE
	NOT EXISTS(SELECT * FROM Role WHERE Name = 'Anonymous')
