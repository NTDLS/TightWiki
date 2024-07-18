DELETE FROM [PageRevision] WHERE PageId = @PageId AND Revision = @Revision;
DELETE FROM [PageRevisionAttachment] WHERE PageId = @PageId AND PageRevision = @Revision;
DELETE FROM [DeletionMeta] WHERE PageId = @PageId AND Revision = @Revision;
