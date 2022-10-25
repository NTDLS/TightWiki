CREATE TABLE #tmp_23c74f4845a94330a85e979b04d772a1 ([Id] [int],[Name] [nvarchar](128),[Navigation] [nvarchar](128),[Description] [nvarchar](max),[Revision] [int],[CreatedByUserId] [int],[CreatedDate] [datetime],[ModifiedByUserId] [int],[ModifiedDate] [datetime])
GO

INSERT INTO #tmp_23c74f4845a94330a85e979b04d772a1 ([Name],[Navigation],[Description],[Revision],[CreatedByUserId],[CreatedDate],[ModifiedByUserId],[ModifiedDate]) VALUES
('Home','Home','The home page',23,1,'1/1/1900 12:00:00 AM',7,'10/25/2022 7:21:27 PM'),
('Function Calls Wiki Help','Function_Calls_Wiki_Help','Outlines the various nuances, formatting for calling functions.',9,1,'9/16/2022 10:33:14 AM',7,'10/25/2022 7:21:27 PM'),
('Development Notes','Development_Notes','Landry list of implementation ideas.',7,1,'9/16/2022 3:41:43 PM',7,'10/25/2022 7:21:27 PM'),
('Images Wiki Help','Images_Wiki_Help','How to display attached and or external images on pages.',8,1,'9/22/2022 12:28:16 PM',7,'10/25/2022 7:21:27 PM'),
('Literals Wiki Help','Literals_Wiki_Help','Literal blocks for displaying verbatim content',8,1,'9/22/2022 4:21:38 PM',7,'10/25/2022 7:21:27 PM'),
('Panels Wiki Help','Panels_Wiki_Help','Panels are blocks of content with defined headers.',23,1,'9/23/2022 11:25:13 AM',7,'10/25/2022 7:21:27 PM'),
('Markup Wiki Help','Markup_Wiki_Help','Basic markup such as bold, italics, underline, etc.',20,1,'9/23/2022 11:25:33 AM',7,'10/25/2022 7:21:27 PM'),
('Processing Instructions Wiki Help','Processing_Instructions_Wiki_Help','Page flags for specific needs such as deletion, review, draft, etc.',10,1,'9/23/2022 11:25:47 AM',7,'10/25/2022 7:21:27 PM'),
('Headings Wiki Help','Headings_Wiki_Help','Headings are used to define the outline of a page.',9,1,'9/23/2022 11:25:56 AM',7,'10/25/2022 7:21:27 PM'),
('Links Wiki Help','Links_Wiki_Help','Linking between wiki pages and external content.',9,1,'9/23/2022 5:59:56 PM',7,'10/25/2022 7:21:27 PM'),
('Bullets Wiki Help','Bullets_Wiki_Help','Order, categorize and sub-categorize items on a page.',9,1,'9/23/2022 9:28:06 PM',7,'10/25/2022 7:21:27 PM'),
('Glossary Wiki Help','Glossary_Wiki_Help','Glossaries and text searches for showing related information.',8,1,'9/23/2022 10:22:54 PM',7,'10/25/2022 7:21:27 PM'),
('Attachemnts Wiki Help','Attachemnts_Wiki_Help','How to attach and display files for download.',9,1,'9/26/2022 5:58:29 PM',7,'10/25/2022 7:21:27 PM'),
('Whitespace Wiki Help','Whitespace_Wiki_Help','How the wiki handles whitespace and what you can do to manipulate it.',9,1,'9/28/2022 3:49:24 PM',7,'10/25/2022 7:21:27 PM'),
('Include:Experimental','IncludeExperimental','Include telling readers that the contents is experimental.',9,1,'9/28/2022 6:04:07 PM',7,'10/25/2022 7:21:27 PM'),
('Functions Wiki Help','Functions_Wiki_Help','Various functions used for content manipulation, searching and and formatting.',14,1,'9/28/2022 8:10:38 PM',7,'10/25/2022 7:21:27 PM'),
('Fallout Heads','Fallout_Heads','Heads, from fallout - need we say more?',7,1,'9/30/2022 1:11:34 PM',7,'10/25/2022 7:21:27 PM'),
('Wiki Page Does Not Exist','Wiki_Page_Does_Not_Exist','This is the content that is displayed when a wiki pages is requested that does not exist.',7,1,'9/30/2022 1:29:11 PM',1,'10/25/2022 7:21:27 PM'),
('Wiki Default Page','Wiki_Default_Page','This pages content will be used as the default content for new pages when they are created.',7,1,'9/30/2022 2:11:18 PM',1,'10/25/2022 7:21:27 PM'),
('Wiki Help','Wiki_Help','All the wiki help, all in one place.',8,1,'10/4/2022 7:21:20 PM',7,'10/25/2022 7:21:27 PM'),
('Wiki Page Revision Does Not Exist','Wiki_Page_Revision_Does_Not_Exist','This is the content that is displayed when a wiki page revision is requested that does not exist.',7,1,'10/5/2022 2:55:18 PM',1,'10/25/2022 7:21:27 PM'),
('Include:Do Not Modify','IncludeDo_Not_Modify','Include asking reader not to modify the page.',10,1,'10/6/2022 3:31:19 PM',7,'10/25/2022 7:21:27 PM'),
('TightWiki Media','TightWiki_Media','Page for official media attachments.',9,1,'10/7/2022 7:14:51 PM',7,'10/25/2022 7:31:06 PM'),
('Code syntax-highlighting Wiki Help','Code_syntax-highlighting_Wiki_Help','Format preservation and syntax highlighting for code snippets.',10,1,'10/10/2022 3:35:42 PM',7,'10/25/2022 7:21:27 PM'),
('Recently Modified','Recently_Modified','A list of recently modified pages.',15,7,'10/17/2022 8:38:36 PM',7,'10/25/2022 7:21:27 PM'),
('Embed YouTube Video','Embed_YouTube_Video','How to embed a youtube video into your wiki page.',7,7,'10/19/2022 4:40:38 PM',7,'10/25/2022 7:21:27 PM'),
('Sandbox','Sandbox','A place to try out new things.',10,7,'10/21/2022 2:47:20 PM',7,'10/25/2022 7:21:27 PM'),
('Jumbotron Wiki Help','Jumbotron_Wiki_Help','Create large box. Can contain other containers.',6,7,'10/24/2022 7:58:59 PM',7,'10/25/2022 7:21:27 PM'),
('Blocks Wiki Help','Blocks_Wiki_Help','Blocks are title-less blocks of wikiformatted content.',10,7,'10/24/2022 7:59:00 PM',7,'10/25/2022 7:21:27 PM'),
('Alerts Wiki Help','Alerts_Wiki_Help','Alerts help call attention to important page details.',7,7,'10/24/2022 7:59:01 PM',7,'10/25/2022 7:21:32 PM'),
('Wiki About','Wiki_About','The TightWiki origin story, motivations and basic principals.',20,7,'10/25/2022 7:35:42 PM',7,'10/25/2022 7:51:38 PM')
GO
ALTER TABLE [dbo].[Page] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[Page] (
	[Name],[Navigation],[Description],[Revision],[CreatedByUserId],[CreatedDate],[ModifiedByUserId],[ModifiedDate])
SELECT
	[Name],[Navigation],[Description],[Revision],[CreatedByUserId],[CreatedDate],[ModifiedByUserId],[ModifiedDate]
FROM #tmp_23c74f4845a94330a85e979b04d772a1 as S
ALTER TABLE [dbo].[Page] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_23c74f4845a94330a85e979b04d772a1
GO
