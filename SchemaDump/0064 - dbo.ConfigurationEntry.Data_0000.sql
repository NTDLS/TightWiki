CREATE TABLE #tmp_27ae8c5ef62c41e1ad35c4a2acc28230 ([Id] [int],[ConfigurationGroupId] [int],[Name] [nvarchar](128),[Value] [nvarchar](255),[DataTypeId] [int],[Description] [nvarchar](1000),[IsEncrypted] [bit])
GO

INSERT INTO #tmp_27ae8c5ef62c41e1ad35c4a2acc28230 ([ConfigurationGroupId],[Name],[Value],[DataTypeId],[Description],[IsEncrypted]) VALUES
(1,'Brand Image (Small)','/images/SharpWiki Icon 32.png',2,'The brand image of the wiki, this is displayed on the top of all wiki pages.',0),
(1,'Copyright','Copyright &copy; NetworkDLS 20xx',2,'The copyright of the wiki, this is shown at the botton of all pages.',0),
(1,'Default Country','US',2,'The country to use for the site culture when no one is logged in or loggedin as guest.',0),
(1,'Default TimeZone','Eastern Standard Time',2,'The timezone to use for the site culture when no one is logged in or loggedin as guest.',0),
(1,'FooterBlurb','SharpWiki is a free and opensource .NET MVC Wiki built on top of SQL Server and various other Microsoft technologies. Feel free to tweak, contort and otherwise use as you desire. Enjoy!',2,'The footer of the wiki, this is shown at the botton of all pages.',0),
(1,'Name','SharpWiki',2,'The name of the wiki, this is displayed everwhere.',0),
(1,'New Page Template','Wiki Default Page',2,'the name of the wiki page to use as the default content when new wiki pages are created.',0),
(1,'Page Not Exists Page','Wiki Page Does Not Exist',2,'The name of the wiki page to display when a non existing page is requested.',0),
(1,'Revision Does Not Exists Page','Wiki Page Revision Does Not Exist',2,'The name of the wiki page to display when a non existing page revision is requested.',0),
(2,'Word Exclusions','of,it,i,is,or,and,but,of,the,a,for,also,be,it,as,that,this,it,to,on,are,if,in',2,'When building a search index, these words will be excluded from indexing. Comma seperated.',0),
(4,'Allow Guest','0',3,'If set, visitors do not need to sign in to interact with the wiki. They will be automatically signed in as the guest.',0),
(4,'Allow Signup','1',3,'If set, users will be allowed to singup and create their own account.',0),
(4,'Default Country','US',2,'The default country to use for new users.',0),
(4,'Default Signup Role','Contributor',2,'The default role to assign users when they singup.',0),
(4,'Default TimeZone','Eastern Standard Time',2,'The default timezone to use for new users.',0),
(4,'Guest Account','Guest',2,'The name of the account to use when a user is logged in a a guest.',0),
(4,'Request Email Verification','1',3,'If set, en email will be sent to request email verification.',0),
(4,'Require Email Verification','1',3,'If set, users can not login until their email addresses are verified.',0),
(5,'SMTP.Address','smtp.gmail.com',2,'The SMTP server ip/host to use when sending email.',0),
(5,'SMTP.From Display Name','SharpWiki',2,'The display name to show for all email communications.',0),
(5,'SMTP.Password','ZsiEDQQCjSs1MugrNB2ODSEHjLSad8DZJXEORjb5Qng=',2,'The SMTP password to use when sending email.',1),
(5,'SMTP.Port','587',2,'The SMTP port to use when sending email.',0),
(5,'SMTP.UserName','ASAPWikiMail@Gmail.com',2,'The account name to use for all email communications.',0)
GO
ALTER TABLE [dbo].[ConfigurationEntry] NOCHECK CONSTRAINT ALL
GO
UPDATE T SET 
T.[Value] = S.[Value],
T.[DataTypeId] = S.[DataTypeId],
T.[Description] = S.[Description],
T.[IsEncrypted] = S.[IsEncrypted]
FROM [dbo].[ConfigurationEntry] as T
INNER JOIN #tmp_27ae8c5ef62c41e1ad35c4a2acc28230 as S
ON T.[ConfigurationGroupId] = S.[ConfigurationGroupId] AND T.[Name] = S.[Name]
GO
INSERT INTO [dbo].[ConfigurationEntry] (
	[ConfigurationGroupId],[Name],[Value],[DataTypeId],[Description],[IsEncrypted])
SELECT
	[ConfigurationGroupId],[Name],[Value],[DataTypeId],[Description],[IsEncrypted]
FROM #tmp_27ae8c5ef62c41e1ad35c4a2acc28230 as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[ConfigurationEntry] as T
	WHERE T.[ConfigurationGroupId] = S.[ConfigurationGroupId] AND T.[Name] = S.[Name]
)
ALTER TABLE [dbo].[ConfigurationEntry] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_27ae8c5ef62c41e1ad35c4a2acc28230
GO
