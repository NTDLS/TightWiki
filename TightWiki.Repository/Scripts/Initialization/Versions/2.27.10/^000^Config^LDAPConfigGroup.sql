INSERT INTO ConfigurationGroup(Name, Description)
SELECT 'LDAP Authentication', 'Configuration for Active Directory authentication using LDAP'
WHERE NOT EXISTS(SELECT 1 FROM ConfigurationGroup WHERE Name = 'LDAP Authentication');
