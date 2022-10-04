SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ConfigurationEntry]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ConfigurationEntry](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ConfigurationGroupId] [int] NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Value] [nvarchar](255) NULL,
	[DataTypeId] [int] NOT NULL,
	[Description] [nvarchar](1000) NULL,
	[IsEncrypted] [bit] NOT NULL
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_ConfigurationEntry_IsEncrypted]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[ConfigurationEntry] ADD  CONSTRAINT [DF_ConfigurationEntry_IsEncrypted]  DEFAULT ((0)) FOR [IsEncrypted]
END

GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConfigurationEntry]') AND name = N'PK_ConfigurationEntry')
ALTER TABLE [dbo].[ConfigurationEntry] ADD  CONSTRAINT [PK_ConfigurationEntry] PRIMARY KEY CLUSTERED 
(
	[ConfigurationGroupId] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]