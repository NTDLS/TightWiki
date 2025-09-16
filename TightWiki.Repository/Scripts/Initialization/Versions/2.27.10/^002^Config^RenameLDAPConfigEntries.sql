UPDATE
	ConfigurationEntry
SET
	Name = Replace(Name, 'LDAP:', 'LDAP :')
WHERE
	Name LIKE 'LDAP%'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'LDAP Authentication')
	