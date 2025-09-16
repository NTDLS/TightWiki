UPDATE
	ConfigurationEntry
SET
	ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'LDAP Authentication')
WHERE
	Name LIKE 'LDAP%'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication')
	