DELETE FROM
	ConfigurationEntry
WHERE
	Name = 'Theme'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization')
