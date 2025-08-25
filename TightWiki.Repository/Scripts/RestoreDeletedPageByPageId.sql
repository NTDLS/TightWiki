--Restore:
INSERT INTO [Page](Id,Name,Namespace,Navigation,Description,Revision,CreatedByUserId,CreatedDate,ModifiedByUserId,ModifiedDate)
SELECT Id,Name,Namespace,Navigation,Description,Revision,CreatedByUserId,CreatedDate,ModifiedByUserId,ModifiedDate FROM deletedpages_db.[Page] WHERE Id = @PageId;

INSERT INTO [PageRevision](PageId,Name,Namespace,Description,Body,Revision,ChangeSummary,ModifiedByUserId,ModifiedDate,DataHash)
SELECT PageId,Name,Namespace,Description,Body,Revision,ChangeSummary,ModifiedByUserId,ModifiedDate,DataHash FROM deletedpages_db.[PageRevision] WHERE PageId = @PageId;

INSERT INTO [PageFile](Id,PageId,Name,Navigation,Revision,CreatedDate)
SELECT Id,PageId,Name,Navigation,Revision,CreatedDate FROM deletedpages_db.[PageFile] WHERE PageId = @PageId;

INSERT INTO [PageFileRevision](PageFileId,ContentType,Size,CreatedByUserId,CreatedDate,Data,Revision,DataHash)
SELECT PageFileId,ContentType,Size,CreatedByUserId,CreatedDate,Data,Revision,DataHash FROM deletedpages_db.PageFileRevision WHERE PageFileId IN (SELECT Id FROM deletedpages_db.[PageFile] WHERE PageId = @PageId);

INSERT INTO [PageRevisionAttachment](PageId,PageFileId,FileRevision,PageRevision)
SELECT PageId,PageFileId,FileRevision,PageRevision FROM deletedpages_db.[PageRevisionAttachment] WHERE PageId = @PageId;

INSERT INTO [PageComment](Id,PageId,CreatedDate,UserId,Body)
SELECT Id,PageId,CreatedDate,UserId,Body FROM deletedpages_db.[PageComment] WHERE PageId = @PageId;

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

