INSERT INTO Role
(
	Name,
	Description
)
SELECT
	'Guest',
	'Guests are users which have not logged.'
WHERE
	NOT EXISTS(SELECT * FROM Role WHERE Name = 'Guest')
