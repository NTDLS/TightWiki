-- Deleting non-current page revisions
DELETE FROM PageRevision
WHERE EXISTS (
    SELECT 1
    FROM (
        SELECT PageId, MAX(Revision) AS MaxRevision
        FROM PageRevision
        GROUP BY PageId
    ) AS MostRecent
    WHERE PageRevision.PageId = MostRecent.PageId
    AND PageRevision.Revision < MostRecent.MaxRevision
);

-- Deleting non-current attachments.
DELETE FROM PageRevisionAttachment
WHERE EXISTS (
    SELECT 1
    FROM (
        SELECT PageFileId, PageId, MAX(FileRevision) AS MaxFileRevision
        FROM PageRevisionAttachment
        GROUP BY PageId, PageFileId
    ) AS MostRecent
    WHERE PageRevisionAttachment.PageFileId = MostRecent.PageFileId
    AND PageRevisionAttachment.PageId = MostRecent.PageId
    AND PageRevisionAttachment.FileRevision < MostRecent.MaxFileRevision
);

-- Deleting non-current page revision attachments
DELETE FROM PageRevisionAttachment
WHERE EXISTS (
    SELECT 1
    FROM (
        SELECT PageFileId, PageId, MAX(PageRevision) AS MaxPageRevision
        FROM PageRevisionAttachment
        GROUP BY PageId, PageFileId
    ) AS MostRecent
    WHERE PageRevisionAttachment.PageFileId = MostRecent.PageFileId
    AND PageRevisionAttachment.PageId = MostRecent.PageId
    AND PageRevisionAttachment.PageRevision < MostRecent.MaxPageRevision
);

-- Deleting non-current page file revisions.
DELETE FROM PageFileRevision
WHERE EXISTS (
    SELECT 1
    FROM (
        SELECT PageFileId, MAX(Revision) AS MaxPageRevision
        FROM PageFileRevision
        GROUP BY PageFileId
    ) AS MostRecent
    WHERE PageFileRevision.PageFileId = MostRecent.PageFileId
    AND PageFileRevision.Revision < MostRecent.MaxPageRevision
);

-- Delete orphaned PageFileRevision
DELETE FROM PageFileRevision
WHERE PageFileId NOT IN (
    SELECT PageFileId FROM PageRevisionAttachment
);

-- Delete orphaned PageFile
DELETE FROM PageFile
WHERE Id NOT IN (
    SELECT PageFileId FROM PageRevisionAttachment
);

-- Assuming everything else worked, lets set all of the revisions back to 1.
UPDATE [Page] SET Revision = 1;
UPDATE PageRevision SET Revision = 1;
UPDATE PageRevisionAttachment SET PageRevision = 1, FileRevision = 1;
UPDATE PageFileRevision SET Revision = 1;
UPDATE PageFile SET Revision = 1;
