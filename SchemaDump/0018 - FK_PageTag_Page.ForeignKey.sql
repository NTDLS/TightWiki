IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageTag_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageTag]'))
ALTER TABLE [dbo].[PageTag]  WITH CHECK ADD  CONSTRAINT [FK_PageTag_Page] FOREIGN KEY([PageId])
REFERENCES [dbo].[Page] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageTag_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageTag]'))
ALTER TABLE [dbo].[PageTag] CHECK CONSTRAINT [FK_PageTag_Page]