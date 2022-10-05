SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PageRevision]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PageRevision](
	[PageId] [int] NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[Revision] [int] NOT NULL,
	[ModifiedByUserId] [int] NOT NULL,
	[ModifiedDate] [datetime] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PageRevision]') AND name = N'PK_PageRevision')
ALTER TABLE [dbo].[PageRevision] ADD  CONSTRAINT [PK_PageRevision] PRIMARY KEY CLUSTERED 
(
	[PageId] ASC,
	[Revision] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]