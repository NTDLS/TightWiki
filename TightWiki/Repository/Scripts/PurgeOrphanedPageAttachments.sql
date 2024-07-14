--Delete orphaned PageFileRevision.
DELETE FROM PageFileRevision
WHERE PageFileId IN (
    SELECT
        PFR.PageFileId
    FROM
        PageFileRevision as PFR
    INNER JOIN PageFile as PF
        ON PF.Id = PFR.PageFileId
    INNER JOIN Page as P
        ON P.Id = PF.PageId
    LEFT OUTER JOIN PageRevisionAttachment as PRA
        ON PRA.PageFileId = PFR.PageFileId
        AND PRA.FileRevision = PFR.Revision
    WHERE
        PRA.PageFileId IS NULL
);

--Delete orphaned PageFile.
DELETE FROM PageFile
WHERE Id NOT IN (SELECT PageFileId FROM PageFileRevision);
