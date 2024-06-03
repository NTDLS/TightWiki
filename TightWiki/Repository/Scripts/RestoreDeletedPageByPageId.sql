--Restore:
INSERT INTO [PageComment] SELECT * FROM deletedpages_db.[PageComment] WHERE PageId = @PageId;
INSERT INTO [PageRevision] SELECT * FROM deletedpages_db.[PageRevision] WHERE PageId = @PageId;
INSERT INTO [PageRevisionAttachment] SELECT * FROM deletedpages_db.[PageRevisionAttachment] WHERE PageId = @PageId;
INSERT INTO [PageFileRevision] SELECT * FROM deletedpages_db.PageFileRevision WHERE PageFileId IN (SELECT Id FROM deletedpages_db.[PageFile] WHERE PageId = @PageId);
INSERT INTO [PageFile] SELECT * FROM deletedpages_db.[PageFile] WHERE PageId = @PageId;
INSERT INTO [Page] SELECT * FROM deletedpages_db.[Page] WHERE Id = @PageId;

--Cleanup
DELETE FROM deletedpages_db.DeletionMeta WHERE PageId = @PageId;

DELETE FROM deletedpages_db.[PageTag] WHERE PageId = @PageId;
DELETE FROM deletedpages_db.[PageToken] WHERE PageId = @PageId;
DELETE FROM deletedpages_db.[PageProcessingInstruction] WHERE PageId = @PageId;
DELETE FROM deletedpages_db.[PageComment] WHERE PageId = @PageId;
DELETE FROM deletedpages_db.[PageRevision] WHERE PageId = @PageId;
DELETE FROM deletedpages_db.[PageRevisionAttachment] WHERE PageId = @PageId;
DELETE FROM deletedpages_db.PageFileRevision WHERE PageFileId IN (SELECT Id FROM deletedpages_db.[PageFile] WHERE PageId = @PageId);
DELETE FROM deletedpages_db.[PageFile] WHERE PageId = @PageId;
DELETE FROM deletedpages_db.[Page] WHERE Id = @PageId;
