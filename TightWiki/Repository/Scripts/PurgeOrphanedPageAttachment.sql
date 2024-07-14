--Delete orphaned PageFileRevision.
DELETE FROM PageFileRevision WHERE PageFileId = @PageFileId;

--Delete orphaned PageFile.
DELETE FROM PageFile
WHERE Id = @PageFileId
AND Id NOT IN (SELECT PFR.PageFileId FROM PageFileRevision as PFR WHERE PFR.PageFileId = @PageFileId);
