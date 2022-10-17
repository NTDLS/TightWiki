IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageRevisionAttachment_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageRevisionAttachment]'))
ALTER TABLE [dbo].[PageRevisionAttachment]  WITH CHECK ADD  CONSTRAINT [FK_PageRevisionAttachment_Page] FOREIGN KEY([PageId])
REFERENCES [dbo].[Page] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageRevisionAttachment_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageRevisionAttachment]'))
ALTER TABLE [dbo].[PageRevisionAttachment] CHECK CONSTRAINT [FK_PageRevisionAttachment_Page]