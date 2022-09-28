CREATE TABLE #tmp_37d67df1da774eb0981a8500c05141b9 ([Id] [int],[EmailAddress] [nvarchar](128),[DisplayName] [nvarchar](128),[PasswordHash] [nvarchar](128))
GO

INSERT INTO #tmp_37d67df1da774eb0981a8500c05141b9 ([EmailAddress],[DisplayName],[PasswordHash]) VALUES
('Admin@AsapWiki.com','admin','e20f2f0d2ba2d1eb8e9509ac46c88f819f2de0d7f9314390c6ee3e12bead8f8d'),
('josh@ntdls.com','Josh Patterson','e20f2f0d2ba2d1eb8e9509ac46c88f819f2de0d7f9314390c6ee3e12bead8f8d')
GO
ALTER TABLE [dbo].[User] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[User] (
	[EmailAddress],[DisplayName],[PasswordHash])
SELECT
	[EmailAddress],[DisplayName],[PasswordHash]
FROM #tmp_37d67df1da774eb0981a8500c05141b9 as S
ALTER TABLE [dbo].[User] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_37d67df1da774eb0981a8500c05141b9
GO
