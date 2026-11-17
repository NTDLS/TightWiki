INSERT INTO ConfigurationEntry (ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality'),
	'Enable MCP',
	'0',
	3,
	'Whether AI clients can read the wiki through the MCP endpoint (/mcp) using a personal API token. Requires Enable API. Default: Disabled.',
	0,
	1
WHERE NOT EXISTS(
	SELECT 1 FROM ConfigurationEntry
	WHERE Name = 'Enable MCP'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality'));
