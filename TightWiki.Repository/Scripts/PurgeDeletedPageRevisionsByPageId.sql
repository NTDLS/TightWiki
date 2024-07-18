DELETE FROM [PageRevision] WHERE PageId = @PageId;
DELETE FROM [PageRevisionAttachment] WHERE PageId = @PageId;
DELETE FROM [DeletionMeta] WHERE PageId = @PageId;
