--Cleanup
DELETE FROM DeletionMeta WHERE PageId = @PageId;

DELETE FROM [PageTag] WHERE PageId = @PageId;
DELETE FROM [PageToken] WHERE PageId = @PageId;
DELETE FROM [PageProcessingInstruction] WHERE PageId = @PageId;
DELETE FROM [PageComment] WHERE PageId = @PageId;
DELETE FROM [PageRevision] WHERE PageId = @PageId;
DELETE FROM [PageRevisionAttachment] WHERE PageId = @PageId;
DELETE FROM PageFileRevision WHERE PageFileId IN (SELECT Id FROM [PageFile] WHERE PageId = @PageId);
DELETE FROM [PageFile] WHERE PageId = @PageId;
DELETE FROM [Page] WHERE Id = @PageId;
