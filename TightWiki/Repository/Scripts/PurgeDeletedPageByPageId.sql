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

