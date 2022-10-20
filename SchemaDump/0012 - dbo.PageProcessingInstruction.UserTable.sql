SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PageProcessingInstruction]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[PageProcessingInstruction](
	[PageId] [int] NOT NULL,
	[Instruction] [nvarchar](128) NOT NULL
) ON [PRIMARY]
END
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PageProcessingInstruction]') AND name = N'PK_ProcessingInstruction')
ALTER TABLE [dbo].[PageProcessingInstruction] ADD  CONSTRAINT [PK_ProcessingInstruction] PRIMARY KEY CLUSTERED 
(
	[PageId] ASC,
	[Instruction] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]