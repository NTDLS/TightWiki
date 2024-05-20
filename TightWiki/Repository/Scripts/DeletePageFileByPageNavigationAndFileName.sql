DELETE FROM PageFile
WHERE Id IN (
    SELECT
        PF.Id
    FROM
        PageFile AS PF
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
);
