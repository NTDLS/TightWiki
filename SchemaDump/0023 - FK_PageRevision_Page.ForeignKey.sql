IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageRevision_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageRevision]'))
ALTER TABLE [dbo].[PageRevision]  WITH CHECK ADD  CONSTRAINT [FK_PageRevision_Page] FOREIGN KEY([PageId])
REFERENCES [dbo].[Page] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageRevision_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageRevision]'))
ALTER TABLE [dbo].[PageRevision] CHECK CONSTRAINT [FK_PageRevision_Page]