DELETE FROM PageRevisionAttachment
WHERE EXISTS (
    SELECT 1
    FROM PageFile AS PF
    INNER JOIN Page AS P
        ON P.Id = PF.PageId
    INNER JOIN PageRevision AS PR
        ON PR.PageId = P.Id
    INNER JOIN PageRevisionAttachment AS PRA
        ON PRA.PageId = P.Id
        AND PRA.PageFileId = PF.Id
        AND PRA.PageRevision = PR.Revision
    INNER JOIN PageFileRevision AS PFR
        ON PFR.PageFileId = PF.Id
        AND PFR.Revision = PRA.FileRevision
    WHERE
        P.Navigation = @PageNavigation
        AND PF.Navigation = @FileNavigation
        AND PageRevisionAttachment.PageId = P.Id
        AND PageRevisionAttachment.PageFileId = PF.Id
        AND PageRevisionAttachment.PageRevision = PR.Revision
);
