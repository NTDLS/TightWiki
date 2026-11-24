INSERT INTO ConfigurationEntry (ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization'),
	'Hide Navbar Brand',
	'0',
	3,
	'Whether to hide the brand (small brand image and wiki name) at the left of the top menu.',
	0,
	1
WHERE NOT EXISTS(
	SELECT 1 FROM ConfigurationEntry
	WHERE Name = 'Hide Navbar Brand'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization'));
