IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageFile_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageFile]'))
ALTER TABLE [dbo].[PageFile]  WITH CHECK ADD  CONSTRAINT [FK_PageFile_Page] FOREIGN KEY([PageId])
REFERENCES [dbo].[Page] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageFile_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageFile]'))
ALTER TABLE [dbo].[PageFile] CHECK CONSTRAINT [FK_PageFile_Page]