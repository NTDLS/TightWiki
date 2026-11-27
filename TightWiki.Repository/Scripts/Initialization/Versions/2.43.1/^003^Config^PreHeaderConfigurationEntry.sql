INSERT INTO ConfigurationEntry (ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE Name = 'HTML Layout'),
	'Pre-Header',
	'',
	5,
	'Visible HTML placed inside the header element, directly above the navigation bar. Becomes part of the sticky header when Fixed Header Menu Position is enabled.',
	0,
	0
WHERE NOT EXISTS(
	SELECT 1 FROM ConfigurationEntry
	WHERE Name = 'Pre-Header'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'HTML Layout'));
