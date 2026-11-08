INSERT INTO ConfigurationEntry (ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality'),
	'Allow API Writes',
	'0',
	3,
	'Whether pages can be created and edited through the API (/v1) and the MCP endpoint (/mcp) using a personal API token. Requires Enable API. Default: Disabled.',
	0,
	1
WHERE NOT EXISTS(
	SELECT 1 FROM ConfigurationEntry
	WHERE Name = 'Allow API Writes'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality'));
