SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PageFile]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PageFile](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PageId] [int] NOT NULL,
	[Name] [nvarchar](500) NOT NULL,
	[ContentType] [nvarchar](100) NOT NULL,
	[Size] [int] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[Data] [varbinary](max) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PageFile]') AND name = N'PK_PageFile')
ALTER TABLE [dbo].[PageFile] ADD  CONSTRAINT [PK_PageFile] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]