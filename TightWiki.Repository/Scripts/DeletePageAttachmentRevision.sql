BEGIN TRANSACTION;

--Detach the file revision from any page revisions.
DELETE FROM PageRevisionAttachment WHERE PageFileId = @PageFileId AND FileRevision = @Revision;

--Delete the file revision.
DELETE FROM PageFileRevision WHERE PageFileId = @PageFileId AND Revision = @Revision;

--Point the file at its latest remaining revision.
UPDATE PageFile
SET Revision = (SELECT MAX(Revision) FROM PageFileRevision WHERE PageFileId = @PageFileId)
WHERE Id = @PageFileId
AND EXISTS (SELECT 1 FROM PageFileRevision WHERE PageFileId = @PageFileId);

--Delete the file if no revisions remain.
DELETE FROM PageFile
WHERE Id = @PageFileId
AND NOT EXISTS (SELECT 1 FROM PageFileRevision WHERE PageFileId = @PageFileId);

COMMIT TRANSACTION;
