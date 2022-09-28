SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Page]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Page](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Navigation] [nvarchar](128) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[CachedBody] [nvarchar](max) NULL,
	[CreatedByUserId] [int] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[ModifiedByUserId] [int] NOT NULL,
	[ModifiedDate] [datetime] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Page_CreatedDate]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Page] ADD  CONSTRAINT [DF_Page_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Page_ModifiedDate]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Page] ADD  CONSTRAINT [DF_Page_ModifiedDate]  DEFAULT (getdate()) FOR [ModifiedDate]
END

GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Page]') AND name = N'IX_Page_Name')
CREATE UNIQUE NONCLUSTERED INDEX [IX_Page_Name] ON [dbo].[Page]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Page]') AND name = N'PK_Page')
ALTER TABLE [dbo].[Page] ADD  CONSTRAINT [PK_Page] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]