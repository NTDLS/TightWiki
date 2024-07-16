INSERT INTO pages_db.[PageRevision]
SELECT * FROM [PageRevision] WHERE PageId = @PageId and Revision = @Revision;

INSERT INTO pages_db.[PageRevisionAttachment]
SELECT * FROM [PageRevisionAttachment] WHERE PageId = @PageId and PageRevision = @Revision;

DELETE FROM DeletionMeta
WHERE PageId = @PageId and Revision = @Revision;

DELETE FROM [PageRevisionAttachment]
WHERE PageId = @PageId and PageRevision = @Revision;

DELETE FROM [PageRevision]
WHERE PageId = @PageId and Revision = @Revision;
