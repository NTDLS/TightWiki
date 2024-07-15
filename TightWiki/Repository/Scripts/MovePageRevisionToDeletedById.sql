INSERT INTO deletedpages_db.[PageRevision] SELECT * FROM [PageRevision] WHERE PageId = @PageId;
INSERT INTO deletedpages_db.[PageRevisionAttachment] SELECT * FROM [PageRevisionAttachment] WHERE PageId = @PageId;

INSERT INTO DeletionMeta(PageId, DeletedByUserId, DeletedDate) SELECT @PageId, @DeletedByUserId, @DeletedDate;

DELETE FROM [PageRevisionAttachment] WHERE PageId = @PageId and PageRevision = @Revision;
DELETE FROM [PageRevision] WHERE PageId = @PageId and Revision = @Revision;
