IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ProcessingInstruction_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageProcessingInstruction]'))
ALTER TABLE [dbo].[PageProcessingInstruction]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingInstruction_Page] FOREIGN KEY([PageId])
REFERENCES [dbo].[Page] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ProcessingInstruction_Page]') AND parent_object_id = OBJECT_ID(N'[dbo].[PageProcessingInstruction]'))
ALTER TABLE [dbo].[PageProcessingInstruction] CHECK CONSTRAINT [FK_ProcessingInstruction_Page]