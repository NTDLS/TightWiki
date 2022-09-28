IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ConfigurationEntry_ConfigurationGroup]') AND parent_object_id = OBJECT_ID(N'[dbo].[ConfigurationEntry]'))
ALTER TABLE [dbo].[ConfigurationEntry]  WITH NOCHECK ADD  CONSTRAINT [FK_ConfigurationEntry_ConfigurationGroup] FOREIGN KEY([ConfigurationGroupId])
REFERENCES [dbo].[ConfigurationGroup] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ConfigurationEntry_ConfigurationGroup]') AND parent_object_id = OBJECT_ID(N'[dbo].[ConfigurationEntry]'))
ALTER TABLE [dbo].[ConfigurationEntry] CHECK CONSTRAINT [FK_ConfigurationEntry_ConfigurationGroup]