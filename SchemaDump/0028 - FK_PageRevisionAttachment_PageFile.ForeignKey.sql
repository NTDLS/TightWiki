IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageRevisionAttachment_PageFile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageRevisionAttachment]'))
ALTER TABLE [dbo].[PageRevisionAttachment]  WITH CHECK ADD  CONSTRAINT [FK_PageRevisionAttachment_PageFile] FOREIGN KEY([PageFileId])
REFERENCES [dbo].[PageFile] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageRevisionAttachment_PageFile]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageRevisionAttachment]'))
ALTER TABLE [dbo].[PageRevisionAttachment] CHECK CONSTRAINT [FK_PageRevisionAttachment_PageFile]