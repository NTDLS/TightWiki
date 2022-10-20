CREATE TABLE #tmp_34113b88cd5340a383cd60a1c14bbb22 ([Id] [int],[ConfigurationGroupId] [int],[Name] [nvarchar](128),[Value] [nvarchar](max),[DataTypeId] [int],[Description] [nvarchar](1000),[IsEncrypted] [bit])
GO

INSERT INTO #tmp_34113b88cd5340a383cd60a1c14bbb22 ([ConfigurationGroupId],[Name],[Value],[DataTypeId],[Description],[IsEncrypted]) VALUES
(1,'Address','http://localhost',2,'the address of your wiki.',0),
(1,'Brand Image (Small)','/images/TightWiki Icon 32.png',2,'The brand image of the wiki, this is displayed on the top of all wiki pages.',0),
(1,'Copyright','Copyright &copy; NetworkDLS 2022',2,'The copyright of the wiki, this is shown at the botton of all pages.',0),
(1,'Default Country','US',2,'The country to use for the site culture when no one is logged in or loggedin as guest.',0),
(1,'Default TimeZone','Eastern Standard Time',2,'The timezone to use for the site culture when no one is logged in or loggedin as guest.',0),
(1,'FooterBlurb','TightWiki is a free and opensource .NET MVC Wiki built on top of SQL Server and various other Microsoft technologies. Feel free to tweak, contort and otherwise use as you desire. Enjoy!',2,'The footer of the wiki, this is shown at the botton of all pages.',0),
(1,'Name','TightWiki',2,'The name of the wiki, this is displayed everwhere.',0),
(1,'New Page Template','Wiki Default Page',2,'the name of the wiki page to use as the default content when new wiki pages are created.',0),
(1,'Page Not Exists Page','Wiki Page Does Not Exist',2,'The name of the wiki page to display when a non existing page is requested.',0),
(1,'Pagination Size','20',1,'the number of items to return when pagination is used.',0),
(1,'Revision Does Not Exists Page','Wiki Page Revision Does Not Exist',2,'The name of the wiki page to display when a non existing page revision is requested.',0),
(2,'Minimum Match Score',null,4,'Value between 0.0 and 1.0 that determins how strong of a match a page search needs to be brfore it it returned in search results.',0),
(2,'Split Camel Case','1',3,'Whether to split CamelCasedStrings when parsing pages for search tokens. This can be helpful when pages contain alot of programming/code content.',0),
(2,'Word Exclusions','do,of,it,i,is,or,and,but,of,the,a,for,also,be,it,as,that,this,it,to,on,are,if,in',2,'When building a search index, these words will be excluded from indexing. Comma seperated.',0),
(3,'Include wiki Description in Meta','1',1,'Whether the desciption of the page should be included in the HTML head meta.',0),
(3,'Include wiki Tags in Meta','1',1,'Whether the wiki tags attached to the page should be included in the HTML head meta keywords.',0),
(4,'Account Verification Email Template','<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"><html xmlns="http://www.w3.org/1999/xhtml">	<head>		<meta http-equiv="content-type" content="text/html; charset=utf-8" />		<title>##SUBJECT##</title>		<style>			.body {				max-width: 800px;				min-width: 400px;				background-color: #fbfaf8;				word-wrap: break-word;			}			.confirmCode {				max-width: 800px;				min-width: 400px;				background-color: #f6fff6;				word-wrap: break-word;			}		</style>		  	</head>	<body style="margin:0px;		padding:0px;		border: 0 none;			font-size: 11px;		font-family: Verdana, sans-serif;		background-color: #fbfaf8;">		<table style="width: 100%; margin: 10px auto 50px auto; border: 0px #ffffff solid; background: #fbfaf8; font-size: 14px; font-family: Verdana, sans-serif;" align="center">			<tbody>				<tr>					<td style="padding: 9px; color: #000000; background: #fbfaf8;">					<img src="##SITEADDRESS##/File/Image/TightWiki_Media/TightWiki_Icon_128_png" alt="##SITENAME##" />				</td>				</tr>				<tr style="padding: 18px;">					<td style="padding: 10px; vertical-align: top; background: #fbfaf8">						<div class="body">							##PERSONNAME##,<br />							&nbsp;&nbsp;&nbsp;&nbsp; This email is being sent because someone (presumably you) created an account on ##SITENAME##.<br />							To finish your signup process, click the link below to confirm your email address:							<br /><br /><strong>Confirmation Link:</strong></br>							<font size="3" color="#aa0000">								<div class="confirmCode"><blockquote><a href="##SITEADDRESS##/User/##ACCOUNTNAME##/Confirm/##CODE##">Click here to confirm your email address</a></blockquote></div>							</font>							</br />Thanks,</br />							&nbsp;&nbsp;&nbsp;&nbsp;##SITENAME##						</div>					</td>				</tr>			</tbody>		</table>	</body></html>',2,'HTML email template for new account verification',0),
(4,'Allow Signup','1',3,'If set, users will be allowed to singup and create their own account.',0),
(4,'Default Country','US',2,'The default country to use for new users.',0),
(4,'Default Signup Role','Contributor',2,'The default role to assign users when they singup.',0),
(4,'Default TimeZone','Eastern Standard Time',2,'The default timezone to use for new users.',0),
(4,'Request Email Verification','1',3,'If set, en email will be sent to request email verification.',0),
(4,'Require Email Verification','1',3,'If set, users can not login until their email addresses are verified.',0),
(4,'Reset Password Email Template','<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"><html xmlns="http://www.w3.org/1999/xhtml">	<head>		<meta http-equiv="content-type" content="text/html; charset=utf-8" />		<title>##SUBJECT##</title>		<style>			.body {				max-width: 800px;				min-width: 400px;				background-color: #fbfaf8;				word-wrap: break-word;			}			.confirmCode {				max-width: 800px;				min-width: 400px;				background-color: #f6fff6;				word-wrap: break-word;			}		</style>		  	</head>	<body style="margin:0px;		padding:0px;		border: 0 none;			font-size: 11px;		font-family: Verdana, sans-serif;		background-color: #fbfaf8;">		<table style="width: 100%; margin: 10px auto 50px auto; border: 0px #ffffff solid; background: #fbfaf8; font-size: 14px; font-family: Verdana, sans-serif;" align="center">			<tbody>				<tr>					<td style="padding: 9px; color: #000000; background: #fbfaf8;">					<img src="##SITEADDRESS##/File/Image/TightWiki_Media/TightWiki_Icon_128_png" alt="##SITENAME##" />				</td>				</tr>				<tr style="padding: 18px;">					<td style="padding: 10px; vertical-align: top; background: #fbfaf8">						<div class="body">							##PERSONNAME##,<br />							&nbsp;&nbsp;&nbsp;&nbsp; This email is being sent because someone requested a password reset on ##SITENAME##.<br />							Click the link below to finish resetting your password:							<br /><br /><strong>Confirmation Link:</strong></br>							<font size="3" color="#aa0000">								<div class="confirmCode"><blockquote><a href="##SITEADDRESS##/User/##ACCOUNTNAME##/Reset/##CODE##">Click here to reset your password</a></blockquote></div>							</font>							</br />Thanks,</br />							&nbsp;&nbsp;&nbsp;&nbsp;##SITENAME##						</div>					</td>				</tr>			</tbody>		</table>	</body></html>',2,'HTML email template to use when sending a password reset email.',0),
(5,'Address','smtp.gmail.com',2,'The SMTP server ip/host to use when sending email.',0),
(5,'From Display Name','TightWiki',2,'The display name to show for all email communications.',0),
(5,'Password','ZsiEDQQCjSs1MugrNB2ODSEHjLSad8DZJXEORjb5Qng=',2,'The SMTP password to use when sending email.',1),
(5,'Port','587',2,'The SMTP port to use when sending email.',0),
(5,'Use SSL','1',3,'Whether SMTP should connect using SSL or not.',0),
(5,'Username','ASAPWikiMail@Gmail.com',2,'The account name to use for all email communications.',0),
(6,'Footer','',2,'HTML placed into the footer section of the page.',0),
(6,'Header','',2,'HTML placed into the header section of the HTML. A usefule place to put scripts and CSS.',0),
(6,'Post-Body','',2,'HTML placed inside the body tag but directly after the other body content.',0),
(6,'Pre-Body','',2,'HTML placed inside the body tag but just before the other content of the page.',0)
GO
ALTER TABLE [dbo].[ConfigurationEntry] NOCHECK CONSTRAINT ALL
GO
UPDATE T SET 
T.[Value] = S.[Value],
T.[DataTypeId] = S.[DataTypeId],
T.[Description] = S.[Description],
T.[IsEncrypted] = S.[IsEncrypted]
FROM [dbo].[ConfigurationEntry] as T
INNER JOIN #tmp_34113b88cd5340a383cd60a1c14bbb22 as S
ON T.[ConfigurationGroupId] = S.[ConfigurationGroupId] AND T.[Name] = S.[Name]
GO
INSERT INTO [dbo].[ConfigurationEntry] (
	[ConfigurationGroupId],[Name],[Value],[DataTypeId],[Description],[IsEncrypted])
SELECT
	[ConfigurationGroupId],[Name],[Value],[DataTypeId],[Description],[IsEncrypted]
FROM #tmp_34113b88cd5340a383cd60a1c14bbb22 as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[ConfigurationEntry] as T
	WHERE T.[ConfigurationGroupId] = S.[ConfigurationGroupId] AND T.[Name] = S.[Name]
)
ALTER TABLE [dbo].[ConfigurationEntry] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_34113b88cd5340a383cd60a1c14bbb22
GO
