SELECT
	Name,
	Namespace,
	Navigation,
	Description,
	Revision,
	DataHash,
	Body
FROM
	DefaultWikiPages
WHERE
	Namespace = @Namespace;

