SELECT
	Id,
	ConfigurationGroupId,
	Name,
	Value,
	DataTypeId,
	Description,
	IsEncrypted,
	IsRequired
FROM
	ConfigurationEntry
ORDER BY
	Id
