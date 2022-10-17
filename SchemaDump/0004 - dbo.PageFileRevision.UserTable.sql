SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PageFileRevision]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PageFileRevision](
	[PageFileId] [int] NOT NULL,
	[ContentType] [nvarchar](100) NOT NULL,
	[Size] [int] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[Data] [varbinary](max) NOT NULL,
	[Revision] [int] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PageFileRevision]') AND name = N'PK_PageFileRevision_1')
ALTER TABLE [dbo].[PageFileRevision] ADD  CONSTRAINT [PK_PageFileRevision_1] PRIMARY KEY CLUSTERED 
(
	[PageFileId] ASC,
	[Revision] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]