SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PageHistory]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PageHistory](
	[PageId] [int] NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[ModifiedByUserId] [int] NOT NULL,
	[ModifiedDate] [datetime] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END