INSERT INTO deletedpages_db.[PageComment](Id,PageId,CreatedDate,UserId,Body)
SELECT Id,PageId,CreatedDate,UserId,Body FROM [PageComment] WHERE PageId = @PageId;

INSERT INTO deletedpages_db.[PageRevision](PageId,Name,Namespace,Description,Body,Revision,ChangeSummary,ModifiedByUserId,ModifiedDate,DataHash)
SELECT PageId,Name,Namespace,Description,Body,Revision,ChangeSummary,ModifiedByUserId,ModifiedDate,DataHash FROM [PageRevision] WHERE PageId = @PageId;

INSERT INTO deletedpages_db.[PageRevisionAttachment](PageId,PageFileId,FileRevision,PageRevision)
SELECT PageId,PageFileId,FileRevision,PageRevision FROM [PageRevisionAttachment] WHERE PageId = @PageId;

INSERT INTO deletedpages_db.[PageFileRevision](PageFileId,ContentType,Size,CreatedByUserId,CreatedDate,Data,Revision,DataHash)
SELECT PageFileId,ContentType,Size,CreatedByUserId,CreatedDate,Data,Revision,DataHash FROM PageFileRevision WHERE PageFileId IN (SELECT Id FROM [PageFile] WHERE PageId = @PageId);

INSERT INTO deletedpages_db.[PageFile](Id,PageId,Name,Navigation,Revision,CreatedDate)
SELECT Id,PageId,Name,Navigation,Revision,CreatedDate FROM [PageFile] WHERE PageId = @PageId;

INSERT INTO deletedpages_db.[Page](Id,Name,Namespace,Navigation,Description,Revision,CreatedByUserId,CreatedDate,ModifiedByUserId,ModifiedDate)
SELECT Id,Name,Namespace,Navigation,Description,Revision,CreatedByUserId,CreatedDate,ModifiedByUserId,ModifiedDate FROM [Page] WHERE Id = @PageId;

--We save these so we can search for deleted pages.
INSERT INTO deletedpages_db.[PageTag](PageId,Tag)
SELECT PageId,Tag FROM [PageTag] WHERE PageId = @PageId;

INSERT INTO deletedpages_db.[PageToken](PageId,Token,Weight,DoubleMetaphone)
SELECT PageId,Token,Weight,DoubleMetaphone FROM [PageToken] WHERE PageId = @PageId;

INSERT INTO deletedpages_db.[PageProcessingInstruction](PageId,Instruction)
SELECT PageId,Instruction FROM [PageProcessingInstruction] WHERE PageId = @PageId;

INSERT INTO DeletionMeta(PageId, DeletedByUserId, DeletedDate) SELECT @PageId, @DeletedByUserId, @DeletedDate;

DELETE FROM [PageComment] WHERE PageId = @PageId;
DELETE FROM [PageFileRevision] WHERE PageFileId IN (SELECT Id FROM [PageFile] WHERE PageId = @PageId);
DELETE FROM [PageRevisionAttachment] WHERE PageId = @PageId;
DELETE FROM [PageFile] WHERE PageId = @PageId;
DELETE FROM [PageProcessingInstruction] WHERE PageId = @PageId;
DELETE FROM [PageReference] WHERE PageId = @PageId;
DELETE FROM [PageReference] WHERE ReferencesPageId = @PageId;
DELETE FROM [PageRevision] WHERE PageId = @PageId;
DELETE FROM [PageTag] WHERE PageId = @PageId;
DELETE FROM [PageToken] WHERE PageId = @PageId;
DELETE FROM [Page] WHERE Id = @PageId;
