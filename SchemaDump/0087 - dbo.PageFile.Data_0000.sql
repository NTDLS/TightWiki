CREATE TABLE #tmp_ad1740c140844bac9a7928ba8eac8559 ([Id] [int],[PageId] [int],[Name] [nvarchar](250),[Navigation] [nvarchar](250),[Revision] [int],[CreatedDate] [datetime])
GO

INSERT INTO #tmp_ad1740c140844bac9a7928ba8eac8559 ([PageId],[Name],[Navigation],[Revision],[CreatedDate]) VALUES
(333,'100.png','100_png',1,'10/5/2022 8:10:33 PM'),
(333,'Cool.png','Cool_png',1,'10/5/2022 8:10:36 PM'),
(333,'Dead.png','Dead_png',1,'10/5/2022 8:10:40 PM'),
(333,'Devil.png','Devil_png',1,'10/5/2022 8:10:43 PM'),
(333,'Geek.png','Geek_png',1,'10/5/2022 8:10:46 PM'),
(333,'Gum.png','Gum_png',1,'10/5/2022 8:10:49 PM'),
(333,'Heart.png','Heart_png',1,'10/5/2022 8:10:52 PM'),
(333,'Injured.png','Injured_png',1,'10/5/2022 8:10:54 PM'),
(333,'Party.png','Party_png',1,'10/5/2022 8:10:56 PM'),
(333,'Sweat.png','Sweat_png',1,'10/5/2022 8:10:59 PM'),
(333,'Unamused.png','Unamused_png',1,'10/5/2022 8:11:07 PM'),
(333,'Whistle.png','Whistle_png',1,'10/5/2022 8:11:16 PM'),
(333,'Wink.png','Wink_png',1,'10/5/2022 8:11:19 PM'),
(1,'TightWiki Logo.png','TightWiki_Logo_png',1,'10/5/2022 8:11:54 PM'),
(336,'TightWiki Logo.png','TightWiki_Logo_png',1,'10/5/2022 8:12:07 PM'),
(345,'TightWiki Icon (multi).ico','TightWiki_Icon_multi_ico',1,'10/7/2022 7:15:00 PM'),
(345,'TightWiki Icon 16.png','TightWiki_Icon_16_png',1,'10/7/2022 7:15:05 PM'),
(345,'TightWiki Icon 32.png','TightWiki_Icon_32_png',1,'10/7/2022 7:15:08 PM'),
(345,'TightWiki Icon 64.png','TightWiki_Icon_64_png',1,'10/7/2022 7:15:12 PM'),
(345,'TightWiki Icon 128.png','TightWiki_Icon_128_png',1,'10/7/2022 7:15:17 PM'),
(345,'TightWiki Logo.png','TightWiki_Logo_png',1,'10/7/2022 7:15:20 PM'),
(5,'TightWiki Icon (multi).ico','TightWiki_Icon_multi_ico',1,'10/10/2022 7:10:41 PM')
GO
ALTER TABLE [dbo].[PageFile] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[PageFile] (
	[PageId],[Name],[Navigation],[Revision],[CreatedDate])
SELECT
	[PageId],[Name],[Navigation],[Revision],[CreatedDate]
FROM #tmp_ad1740c140844bac9a7928ba8eac8559 as S
ALTER TABLE [dbo].[PageFile] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_ad1740c140844bac9a7928ba8eac8559
GO
