IF NOT EXISTS (SELECT * FROM sys.types st JOIN sys.schemas ss ON st.schema_id = ss.schema_id WHERE st.name = N'PageTokenType' AND ss.name = N'dbo')
CREATE TYPE [dbo].[PageTokenType] AS TABLE(
	[PageId] [int] NULL,
	[Token] [nvarchar](128) NULL,
	[Weight] [int] NULL
)