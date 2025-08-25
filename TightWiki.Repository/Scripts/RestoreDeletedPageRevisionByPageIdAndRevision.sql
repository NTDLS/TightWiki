INSERT INTO pages_db.[PageRevision](PageId,Name,Namespace,Description,Body,Revision,ChangeSummary,ModifiedByUserId,ModifiedDate,DataHash)
SELECT PageId,Name,Namespace,Description,Body,Revision,ChangeSummary,ModifiedByUserId,ModifiedDate,DataHash FROM [PageRevision] WHERE PageId = @PageId and Revision = @Revision;

INSERT INTO pages_db.[PageRevisionAttachment](PageId,PageFileId,FileRevision,PageRevision)
SELECT PageId,PageFileId,FileRevision,PageRevision FROM [PageRevisionAttachment] WHERE PageId = @PageId and PageRevision = @Revision;

DELETE FROM DeletionMeta
WHERE PageId = @PageId and Revision = @Revision;

DELETE FROM [PageRevisionAttachment]
WHERE PageId = @PageId and PageRevision = @Revision;

DELETE FROM [PageRevision]
WHERE PageId = @PageId and Revision = @Revision;
