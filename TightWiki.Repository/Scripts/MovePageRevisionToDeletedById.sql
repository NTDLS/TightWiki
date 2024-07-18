INSERT INTO deletedpagerevisions_db.[PageRevision]
	SELECT * FROM [PageRevision] WHERE PageId = @PageId and Revision = @Revision;

INSERT INTO deletedpagerevisions_db.[PageRevisionAttachment]
	SELECT * FROM [PageRevisionAttachment] WHERE PageId = @PageId and PageRevision = @Revision;

INSERT INTO deletedpagerevisions_db.DeletionMeta(PageId, Revision, DeletedByUserId, DeletedDate)
	SELECT @PageId, @Revision, @DeletedByUserId, @DeletedDate;

DELETE FROM [PageRevisionAttachment] WHERE PageId = @PageId and PageRevision = @Revision;
DELETE FROM [PageRevision] WHERE PageId = @PageId and Revision = @Revision;
