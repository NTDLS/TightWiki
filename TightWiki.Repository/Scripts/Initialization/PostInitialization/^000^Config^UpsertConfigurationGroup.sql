INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 1, 'Basic', 'Basic wiki settings such as formatting.'
ON CONFLICT(Name) DO UPDATE SET Description = 'Basic wiki settings such as formatting.';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 2, 'Search', 'Configuration related to searching and indexing.'
ON CONFLICT(Name) DO UPDATE SET Description = 'Configuration related to searching and indexing.';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 3, 'Functionality', 'General wiki functionality.'
ON CONFLICT(Name) DO UPDATE SET Description = 'General wiki functionality.';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 4, 'Membership', 'Membership settings such as defaults for new members and permissions.'
ON CONFLICT(Name) DO UPDATE SET Description = 'Membership settings such as defaults for new members and permissions.';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 5, 'Email', 'EMail and SMTP setting.'
ON CONFLICT(Name) DO UPDATE SET Description = 'EMail and SMTP setting.';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 6, 'HTML Layout', 'HTML layout.'
ON CONFLICT(Name) DO UPDATE SET Description = 'HTML layout.';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 7, 'Performance', 'Performance related settings.'
ON CONFLICT(Name) DO UPDATE SET Description = 'Performance related settings.';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 8, 'Customization', 'Look & Feel customizations.'
ON CONFLICT(Name) DO UPDATE SET Description = 'Look & Feel customizations.';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 9, 'External Authentication', 'External Authentication Providers'
ON CONFLICT(Name) DO UPDATE SET Description = 'External Authentication Providers';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 10, 'Files and Attachments', 'File and Attachment settings.'
ON CONFLICT(Name) DO UPDATE SET Description = 'File and Attachment settings.';
INSERT INTO ConfigurationGroup(Id, Name, Description)
SELECT 12, 'Cookies', 'Authentication and Cookies.'
ON CONFLICT(Name) DO UPDATE SET Description = 'Authentication and Cookies.';
