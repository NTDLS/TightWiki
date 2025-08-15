INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Basic' LIMIT 1), 'Name', 'TightWiki', 2, 'The name of the wiki, this is displayed everywhere.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Name', DataTypeId = 2, Description = 'The name of the wiki, this is displayed everywhere.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'FooterBlurb', 'TightWiki is an aspnet core MVC Wiki built on top of SQLite.', 5, 'The footer of the wiki, this is shown at the bottom of all pages.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'FooterBlurb', DataTypeId = 5, Description = 'The footer of the wiki, this is shown at the bottom of all pages.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Basic' LIMIT 1), 'Copyright', 'Copyright &copy; NetworkDLS 2024', 2, 'The copyright of the wiki, this is shown at the bottom of all pages.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Copyright', DataTypeId = 2, Description = 'The copyright of the wiki, this is shown at the bottom of all pages.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'New Page Template', 'Builtin :: Wiki Default Page', 2, 'the name of the wiki page to use as the default content when new wiki pages are created.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'New Page Template', DataTypeId = 2, Description = 'the name of the wiki page to use as the default content when new wiki pages are created.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Search' LIMIT 1), 'Word Exclusions', 'do,of,it,i,is,or,and,but,of,the,a,for,also,be,it,as,that,this,it,to,on,are,if,in', 2, 'When building a search index, these words will be excluded from indexing. (Comma separated)', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Word Exclusions', DataTypeId = 2, Description = 'When building a search index, these words will be excluded from indexing. (Comma separated)', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Brand Image (Small)', '~/images/TightWiki Icon 32.png', 2, 'The brand image of the wiki, this is displayed on the top of all wiki pages.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Brand Image (Small)', DataTypeId = 2, Description = 'The brand image of the wiki, this is displayed on the top of all wiki pages.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership' LIMIT 1), 'Allow Signup', '1', 3, 'If set, users will be allowed to sing up and create their own account. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Allow Signup', DataTypeId = 3, Description = 'If set, users will be allowed to sing up and create their own account. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Files and Attachments' LIMIT 1), 'Max Attachment File Size', '5242880', 2, 'Maximum size in bytes for a user any page attachment.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Max Attachment File Size', DataTypeId = 2, Description = 'Maximum size in bytes for a user any page attachment.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership' LIMIT 1), 'Default Signup Role', 'Contributor', 2, 'The default role to assign accounts when they sing up. Default: Contributor.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Default Signup Role', DataTypeId = 2, Description = 'The default role to assign accounts when they sing up. Default: Contributor.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Page Not Exists Page', 'Builtin :: Wiki Page Does Not Exist', 2, 'The name of the wiki page to display when a non existing page is requested.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Page Not Exists Page', DataTypeId = 2, Description = 'The name of the wiki page to display when a non existing page is requested.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Email' LIMIT 1), 'Address', 'smtp.gmail.com', 2, 'The SMTP server ip/host to use when sending email.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Address', DataTypeId = 2, Description = 'The SMTP server ip/host to use when sending email.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Email' LIMIT 1), 'Password', 'mPRpPQEBy2yh5neR0dtN46sPY7rqZJxIxRAUIEtit/s=', 2, 'The SMTP password to use when sending email.', 1, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Password', DataTypeId = 2, Description = 'The SMTP password to use when sending email.', IsEncrypted = '1', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Email' LIMIT 1), 'Port', '465', 2, 'The SMTP port to use when sending email. Default: 587.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Port', DataTypeId = 2, Description = 'The SMTP port to use when sending email. Default: 587.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Email' LIMIT 1), 'Username', 'tightwiki@gmail.com', 2, 'The account name to use for all email communications.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Username', DataTypeId = 2, Description = 'The account name to use for all email communications.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Email' LIMIT 1), 'From Display Name', 'TightWiki', 2, 'The display name to show for all email communications.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'From Display Name', DataTypeId = 2, Description = 'The display name to show for all email communications.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership' LIMIT 1), 'Require Email Verification', '0', 3, 'If set, users can not login until their email addresses are verified. If changed, the website will need to be restarted. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Require Email Verification', DataTypeId = 3, Description = 'If set, users can not login until their email addresses are verified. If changed, the website will need to be restarted. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership' LIMIT 1), 'Default Country', 'US', 2, 'The default country to use for new accounts.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Default Country', DataTypeId = 2, Description = 'The default country to use for new accounts.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership' LIMIT 1), 'Default TimeZone', 'Eastern Standard Time', 2, 'The default time zone to use for new accounts.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Default TimeZone', DataTypeId = 2, Description = 'The default time zone to use for new accounts.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Default Country', 'US', 2, 'The country to use for the site culture for guests.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Default Country', DataTypeId = 2, Description = 'The country to use for the site culture for guests.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Default TimeZone', 'Eastern Standard Time', 2, 'The time zone to use for the site culture for guests.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Default TimeZone', DataTypeId = 2, Description = 'The time zone to use for the site culture for guests.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Revision Does Not Exists Page', 'Builtin :: Wiki Page Revision Does Not Exist', 2, 'The name of the wiki page to display when a non existing page revision is requested.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Revision Does Not Exists Page', DataTypeId = 2, Description = 'The name of the wiki page to display when a non existing page revision is requested.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Pagination Size', '20', 1, 'The number of items to return when pagination is used. Default: 20.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Pagination Size', DataTypeId = 1, Description = 'The number of items to return when pagination is used. Default: 20.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'HTML Layout' LIMIT 1), 'Header', '', 5, 'HTML placed into the header section of the HTML. A useful place to put scripts and CSS.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Header', DataTypeId = 5, Description = 'HTML placed into the header section of the HTML. A useful place to put scripts and CSS.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'HTML Layout' LIMIT 1), 'Footer', '', 5, 'HTML placed into the footer section of the page.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Footer', DataTypeId = 5, Description = 'HTML placed into the footer section of the page.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'HTML Layout' LIMIT 1), 'Pre-Body', '', 5, 'HTML placed inside the body tag but just before the other content of the page.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Pre-Body', DataTypeId = 5, Description = 'HTML placed inside the body tag but just before the other content of the page.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'HTML Layout' LIMIT 1), 'Post-Body', '', 5, 'HTML placed inside the body tag but directly after the other body content.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Post-Body', DataTypeId = 5, Description = 'HTML placed inside the body tag but directly after the other body content.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Basic' LIMIT 1), 'Address', 'https://localhost:7053', 2, 'the address of your wiki.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Address', DataTypeId = 2, Description = 'the address of your wiki.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Email' LIMIT 1), 'Use SSL', '1', 3, 'Whether SMTP should connect using SSL or not.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Use SSL', DataTypeId = 3, Description = 'Whether SMTP should connect using SSL or not.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality' LIMIT 1), 'Include wiki Description in Meta', '1', 3, 'Whether the description of the page should be included in the HTML head meta. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Include wiki Description in Meta', DataTypeId = 3, Description = 'Whether the description of the page should be included in the HTML head meta. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality' LIMIT 1), 'Include wiki Tags in Meta', '1', 3, 'Whether the wiki tags attached to the page should be included in the HTML head meta keywords. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Include wiki Tags in Meta', DataTypeId = 3, Description = 'Whether the wiki tags attached to the page should be included in the HTML head meta keywords. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Search' LIMIT 1), 'Split Camel Case', '1', 3, 'Whether to split CamelCasedStrings when parsing pages for search tokens. This can be helpful when pages contain a lot of programming/code content. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Split Camel Case', DataTypeId = 3, Description = 'Whether to split CamelCasedStrings when parsing pages for search tokens. This can be helpful when pages contain a lot of programming/code content. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Search' LIMIT 1), 'Minimum Match Score', '0.60', 4, 'Value between 0.0 and 1.0 that determines how strong of a match a page search needs to be before it it returned in search results. Default: 0.6.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Minimum Match Score', DataTypeId = 4, Description = 'Value between 0.0 and 1.0 that determines how strong of a match a page search needs to be before it it returned in search results. Default: 0.6.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Default Language', 'English', 2, 'The language to use for the site culture for guests.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Default Language', DataTypeId = 2, Description = 'The language to use for the site culture for guests.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership' LIMIT 1), 'Default Language', 'English', 2, 'The default language to use for new accounts.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Default Language', DataTypeId = 2, Description = 'The default language to use for new accounts.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Search' LIMIT 1), 'Allow Fuzzy Matching', '1', 3, 'Whether to allow non-exact matches when searching. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Allow Fuzzy Matching', DataTypeId = 3, Description = 'Whether to allow non-exact matches when searching. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Search' LIMIT 1), 'Include Search on Navbar', '1', 3, 'Whether a search box will be shown on the navigation bar. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Include Search on Navbar', DataTypeId = 3, Description = 'Whether a search box will be shown on the navigation bar. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Performance' LIMIT 1), 'Page Cache Time (Seconds)', '300', 2, 'The number of seconds to cache pages in memory (0 = disabled). Default: 300.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Page Cache Time (Seconds)', DataTypeId = 2, Description = 'The number of seconds to cache pages in memory (0 = disabled). Default: 300.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Default Emoji Height', '24', 2, 'The default height to scale emojis to. The width will be scaled to the same ratio.  Default: 24.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Default Emoji Height', DataTypeId = 2, Description = 'The default height to scale emojis to. The width will be scaled to the same ratio.  Default: 24.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Performance' LIMIT 1), 'Record Compilation Metrics', '1', 3, 'Whether to record page wiki Record compilation metrics to the database. This can be used for performance turning, but can itself cause slowness. Default: Disabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Record Compilation Metrics', DataTypeId = 3, Description = 'Whether to record page wiki Record compilation metrics to the database. This can be used for performance turning, but can itself cause slowness. Default: Disabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Performance' LIMIT 1), 'Cache Memory Limit MB', '1024', 2, 'The memory limit (in megabytes) for the cache. Oldest items will be ejected when limit is reached. Default: 1024.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Cache Memory Limit MB', DataTypeId = 2, Description = 'The memory limit (in megabytes) for the cache. Oldest items will be ejected when limit is reached. Default: 1024.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality' LIMIT 1), 'Enable Page Comments', '1', 3, 'Whether page comments will be enabled. Authentication is required for comment, but all can read. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Enable Page Comments', DataTypeId = 3, Description = 'Whether page comments will be enabled. Authentication is required for comment, but all can read. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality' LIMIT 1), 'Show Comments on Page Footer', '1', 3, 'When comments are enabled, this option causes comments to be shown at the bottom of each page. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Show Comments on Page Footer', DataTypeId = 3, Description = 'When comments are enabled, this option causes comments to be shown at the bottom of each page. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Performance' LIMIT 1), 'Default Profile Recently Modified Count', '10', 1, 'The number of recently modified pages to show on users public profiles. Default: 10.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Default Profile Recently Modified Count', DataTypeId = 1, Description = 'The number of recently modified pages to show on users public profiles. Default: 10.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality' LIMIT 1), 'Show Last Modified on Page Footer', '1', 3, 'Whether to show the last user and date that a page was modified with a link to the public user profile. Default: Enabled.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Show Last Modified on Page Footer', DataTypeId = 3, Description = 'Whether to show the last user and date that a page was modified with a link to the public user profile. Default: Enabled.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'Google : Use Google Authentication', '1', 3, '', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Google : Use Google Authentication', DataTypeId = 3, Description = '', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'Google : ClientId', 'XqeNL24IB2PutHarFwieVZNcz1VX/8pguGM2RLcx6jbVdLXyoJSoBZ1Rvj2BXzimMW+P/2wRleHvYIMYa7UAqDP8pVZC9tZQ77JScUQFs7o=', 2, '', 1, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Google : ClientId', DataTypeId = 2, Description = '', IsEncrypted = '1', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'Google : ClientSecret', 'Wr840pS/aydZaGMyukTN95FLmPKzSfeYS1kLnYZ27aiygOzPz/mhbvDypR2FlTqI', 2, '', 1, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Google : ClientSecret', DataTypeId = 2, Description = '', IsEncrypted = '1', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'Microsoft : Use Microsoft Authentication', '0', 3, '', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Microsoft : Use Microsoft Authentication', DataTypeId = 3, Description = '', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'Microsoft : ClientId', 'OBkvufweSYd0E1zRD5qAuA==', 2, '', 1, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Microsoft : ClientId', DataTypeId = 2, Description = '', IsEncrypted = '1', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'Microsoft : ClientSecret', 'OBkvufweSYd0E1zRD5qAuA==', 2, '', 1, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Microsoft : ClientSecret', DataTypeId = 2, Description = '', IsEncrypted = '1', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership' LIMIT 1), 'Template: Account Verification Email', '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta http-equiv="content-type" content="text/html; charset=utf-8" />
    <title>##SUBJECT##</title>
    <style>
        .body {
            max-width: 800px;
            min-width: 400px;
            background-color: #fbfaf8;
            word-wrap: break-word;
        }

        .confirmCode {
            max-width: 800px;
            min-width: 400px;
            background-color: #f6fff6;
            word-wrap: break-word;
        }
    </style>
</head>
<body style="margin:0px; padding:0px; border: 0 none; font-size: 11px; font-family: Verdana, sans-serif; background-color: #fbfaf8;">
    <table style="width: 100%; margin: 10px auto 50px auto; border: 0px #ffffff solid; background: #fbfaf8; font-size: 14px; font-family: Verdana, sans-serif;" align="center">
        <tbody>
            <tr>
                <td style="padding: 9px; color: #000000; background: #fbfaf8;">
                    <img src="##SITEADDRESS##/File/Image/builtin::media/TightWiki_Icon_128_png" alt="##SITENAME##" />
                </td>
            </tr>
            <tr style="padding: 18px;">
                <td style="padding: 10px; vertical-align: top; background: #fbfaf8">
                    <div class="body">
                        New Member,<br />
                        &nbsp;&nbsp;&nbsp;&nbsp; This email is being sent because someone (presumably you) created an account on ##SITENAME##.<br />
                        To finish your signup process, click the link below to confirm your email address:
                        <br /><br /><strong>Confirmation Link:</strong></br>
                        <font size="3" color="#aa0000">
                            <div class="confirmCode"><blockquote><a href="##CALLBACKURL##">Click here to confirm your email address</a></blockquote></div>
                        </font>
                        <br />Thanks,<br />
                        &nbsp;&nbsp;&nbsp;&nbsp;##SITENAME##
                    </div>
                </td>
            </tr>
        </tbody>
    </table>
</body>
</html>
', 5, 'HTML email template for new account verification', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Template: Account Verification Email', DataTypeId = 5, Description = 'HTML email template for new account verification', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership' LIMIT 1), 'Template: Reset Password Email', '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta http-equiv="content-type" content="text/html; charset=utf-8" />
    <title>##SUBJECT##</title>
    <style>
        .body {
            max-width: 800px;
            min-width: 400px;
            background-color: #fbfaf8;
            word-wrap: break-word;
        }

        .confirmCode {
            max-width: 800px;
            min-width: 400px;
            background-color: #f6fff6;
            word-wrap: break-word;
        }
    </style>
</head>
<body style="margin:0px; padding:0px; border: 0 none; font-size: 11px; font-family: Verdana, sans-serif; background-color: #fbfaf8;">
    <table style="width: 100%; margin: 10px auto 50px auto; border: 0px #ffffff solid; background: #fbfaf8; font-size: 14px; font-family: Verdana, sans-serif;" align="center">
        <tbody>
            <tr>
                <td style="padding: 9px; color: #000000; background: #fbfaf8;">
                    <img src="##SITEADDRESS##/File/Image/builtin::media/TightWiki_Icon_128_png" alt="##SITENAME##" />
                </td>
            </tr>
            <tr style="padding: 18px;">
                <td style="padding: 10px; vertical-align: top; background: #fbfaf8">
                    <div class="body">
                        Member,<br />
                        &nbsp;&nbsp;&nbsp;&nbsp; This email is being sent because someone requested a password reset on ##SITENAME##.<br />
                        Click the link below to finish resetting your password:
                        <br /><br /><strong>Confirmation Link:</strong></br>
                        <font size="3" color="#aa0000">
                            <div class="confirmCode"><blockquote><a href="##CALLBACKURL##">Click here to reset your password</a></blockquote></div>
                        </font>
                        <br />Thanks,<br />
                        &nbsp;&nbsp;&nbsp;&nbsp;##SITENAME##
                    </div>
                </td>
            </tr>
        </tbody>
    </table>
</body>
</html>', 5, 'HTML email template to use when sending a password reset email.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Template: Reset Password Email', DataTypeId = 5, Description = 'HTML email template to use when sending a password reset email.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Theme', 'Light', 2, 'The base theme to use for all site styling.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Theme', DataTypeId = 2, Description = 'The base theme to use for all site styling.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Customization' LIMIT 1), 'Fixed Header Menu Position', '1', 3, 'Whether the top menu should stay in place when scrolling vertically.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Fixed Header Menu Position', DataTypeId = 3, Description = 'Whether the top menu should stay in place when scrolling vertically.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Files and Attachments' LIMIT 1), 'Max Avatar File Size', '1048576', 2, 'Maximum size in bytes for a user avatar image.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Max Avatar File Size', DataTypeId = 2, Description = 'Maximum size in bytes for a user avatar image.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Files and Attachments' LIMIT 1), 'Max Emoji File Size', '524288', 2, 'Maximum size in bytes for an emoji image.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Max Emoji File Size', DataTypeId = 2, Description = 'Maximum size in bytes for an emoji image.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Performance' LIMIT 1), 'Pre-Load Animated Emojis', '0', 3, 'Whether or not the engine should perform a lazy pre-load of all animated emojis on startup.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Pre-Load Animated Emojis', DataTypeId = 3, Description = 'Whether or not the engine should perform a lazy pre-load of all animated emojis on startup.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Functionality' LIMIT 1), 'Enable Public Profiles', '1', 3, 'Whether visitors can view members and their public profiles.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Enable Public Profiles', DataTypeId = 3, Description = 'Whether visitors can view members and their public profiles.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Cookies' LIMIT 1), 'Persist Keys Path', '', 2, 'Filesystem location where persistent cookies are stored for saved session logins.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Persist Keys Path', DataTypeId = 2, Description = 'Filesystem location where persistent cookies are stored for saved session logins.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'Cookies' LIMIT 1), 'Expiraion Hours', '360', 1, 'Number of hours before cookies expire.', 0, 1
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'Expiraion Hours', DataTypeId = 1, Description = 'Number of hours before cookies expire.', IsEncrypted = '0', IsRequired = '1';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'OIDC : Use OIDC Authentication', '0', 3, 'Whether or not OpenID Connect is enabled.', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'OIDC : Use OIDC Authentication', DataTypeId = 3, Description = 'Whether or not OpenID Connect is enabled.', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'OIDC : Authority', '', 2, '', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'OIDC : Authority', DataTypeId = 2, Description = '', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'OIDC : ClientId', '', 2, '', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'OIDC : ClientId', DataTypeId = 2, Description = '', IsEncrypted = '0', IsRequired = '0';
INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = 'External Authentication' LIMIT 1), 'OIDC : ClientSecret', '', 2, '', 0, 0
ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = 'OIDC : ClientSecret', DataTypeId = 2, Description = '', IsEncrypted = '0', IsRequired = '0';
