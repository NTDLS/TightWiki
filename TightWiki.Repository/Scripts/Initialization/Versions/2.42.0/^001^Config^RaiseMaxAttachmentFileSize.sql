--Attachments are now uploaded in pieces and stored on disk, so the old 5MB default limit is raised to 4GB.
--Only wikis still using the old default are changed, a limit chosen by an administrator is kept.
UPDATE
	ConfigurationEntry
SET
	Value = '4294967296'
WHERE
	Name = 'Max Attachment File Size'
	AND Value = '5242880'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'Files and Attachments')
