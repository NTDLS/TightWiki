IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ConfigurationEntry_DataType]') AND parent_object_id = OBJECT_ID(N'[dbo].[ConfigurationEntry]'))
ALTER TABLE [dbo].[ConfigurationEntry]  WITH CHECK ADD  CONSTRAINT [FK_ConfigurationEntry_DataType] FOREIGN KEY([DataTypeId])
REFERENCES [dbo].[DataType] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ConfigurationEntry_DataType]') AND parent_object_id = OBJECT_ID(N'[dbo].[ConfigurationEntry]'))
ALTER TABLE [dbo].[ConfigurationEntry] CHECK CONSTRAINT [FK_ConfigurationEntry_DataType]