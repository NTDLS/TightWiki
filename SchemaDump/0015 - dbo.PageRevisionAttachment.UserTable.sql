SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PageRevisionAttachment]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PageRevisionAttachment](
	[PageId] [int] NOT NULL,
	[PageFileId] [int] NOT NULL,
	[FileRevision] [int] NOT NULL,
	[PageRevision] [int] NOT NULL
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PageRevisionAttachment]') AND name = N'PK_PageRevisionAttachment')
ALTER TABLE [dbo].[PageRevisionAttachment] ADD  CONSTRAINT [PK_PageRevisionAttachment] PRIMARY KEY CLUSTERED 
(
	[PageId] ASC,
	[PageFileId] ASC,
	[FileRevision] ASC,
	[PageRevision] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]