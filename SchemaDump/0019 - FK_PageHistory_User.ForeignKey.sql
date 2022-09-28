IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageHistory_User]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageHistory]'))
ALTER TABLE [dbo].[PageHistory]  WITH CHECK ADD  CONSTRAINT [FK_PageHistory_User] FOREIGN KEY([ModifiedByUserId])
REFERENCES [dbo].[User] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PageHistory_User]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageHistory]'))
ALTER TABLE [dbo].[PageHistory] CHECK CONSTRAINT [FK_PageHistory_User]