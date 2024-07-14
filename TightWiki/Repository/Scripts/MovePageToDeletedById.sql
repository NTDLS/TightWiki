INSERT INTO deletedpages_db.[PageComment] SELECT * FROM [PageComment] WHERE PageId = @PageId;
INSERT INTO deletedpages_db.[PageRevision] SELECT * FROM [PageRevision] WHERE PageId = @PageId;
INSERT INTO deletedpages_db.[PageRevisionAttachment] SELECT * FROM [PageRevisionAttachment] WHERE PageId = @PageId;
INSERT INTO deletedpages_db.[PageFileRevision] SELECT * FROM PageFileRevision WHERE PageFileId IN (SELECT Id FROM [PageFile] WHERE PageId = @PageId);
INSERT INTO deletedpages_db.[PageFile] SELECT * FROM [PageFile] WHERE PageId = @PageId;
INSERT INTO deletedpages_db.[Page] SELECT * FROM [Page] WHERE Id = @PageId;

--We save these so we can search for deleted pages.
INSERT INTO deletedpages_db.[PageTag] SELECT * FROM [PageTag] WHERE PageId = @PageId;
INSERT INTO deletedpages_db.[PageToken] SELECT * FROM [PageToken] WHERE PageId = @PageId;
INSERT INTO deletedpages_db.[PageProcessingInstruction] SELECT * FROM [PageProcessingInstruction] WHERE PageId = @PageId;

INSERT INTO DeletionMeta(PageId, DeletedByUserId, DeletedDate) SELECT @PageId, @DeletedByUserId, @DeletedDate;

DELETE FROM [PageComment] WHERE PageId = 1;
DELETE FROM [PageFileRevision] WHERE PageFileId IN (SELECT Id FROM [PageFile] WHERE PageId = 1);
DELETE FROM [PageRevisionAttachment] WHERE PageId = 1;
DELETE FROM [PageFile] WHERE PageId = 1;
DELETE FROM [PageProcessingInstruction] WHERE PageId = 1;
DELETE FROM [PageReference] WHERE PageId = 1;
DELETE FROM [PageReference] WHERE ReferencesPageId = 1;
DELETE FROM [PageRevision] WHERE PageId = 1;
DELETE FROM [PageTag] WHERE PageId = 1;
DELETE FROM [PageToken] WHERE PageId = 1;
DELETE FROM [Page] WHERE Id = 1;
