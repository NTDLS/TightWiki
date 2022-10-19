IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[TruncateAllPageHistory]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[TruncateAllPageHistory] AS'
END
GO




ALTER PROCEDURE [dbo].[TruncateAllPageHistory]
(
	@Confirm nvarchar(128)	
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	IF(@Confirm = 'YES')
	BEGIN--IF

		BEGIN TRANSACTION

		DELETE
			PR
		FROM
			PageRevision as PR
		INNER JOIN (
			SELECT
				iPR.PageId,
				Max(iPR.Revision) as Revision
			FROM
				PageRevision as iPR
			GROUP BY
				iPR.PageId
			) as MostRecent
			ON PR.PageId = MostRecent.PageId
			AND PR.Revision < MostRecent.Revision

		PRINT 'Deleted ' + Cast(@@rowcount as varchar) + ' page revisions.'

		DELETE
			PRA
		FROM
			PageRevisionAttachment as PRA
		INNER JOIN (
			SELECT
				iPRA.PageFileId,
				iPRA.PageId,
				Max(iPRA.FileRevision) as FileRevision
			FROM
				PageRevisionAttachment as iPRA
			GROUP BY
				iPRA.PageId,
				iPRA.PageFileId
			) as MostRecent
			ON PRA.PageFileId = MostRecent.PageFileId
			AND PRA.PageId = MostRecent.PageId
			AND PRA.FileRevision < MostRecent.FileRevision

		PRINT 'Deleted ' + Cast(@@rowcount as varchar) + ' page file revision associations.'

		DELETE
			PRA
		FROM
			PageRevisionAttachment as PRA
		INNER JOIN (
			SELECT
				iPRA.PageFileId,
				iPRA.PageId,
				Max(iPRA.PageRevision) as PageRevision
			FROM
				PageRevisionAttachment as iPRA
			GROUP BY
				iPRA.PageId,
				iPRA.PageFileId
			) as MostRecent
			ON PRA.PageFileId = MostRecent.PageFileId
			AND PRA.PageId = MostRecent.PageId
			AND PRA.PageRevision < MostRecent.PageRevision

		PRINT 'Deleted ' + Cast(@@rowcount as varchar) + ' page revision file associations.'

		DELETE
			PFR
		FROM
			PageFileRevision as PFR
		INNER JOIN (
			SELECT
				iPFR.PageFileId,
				Max(iPFR.Revision) as Revision
			FROM
				PageFileRevision as iPFR
			GROUP BY
				iPFR.PageFileId
			) as MostRecent
			ON PFR.PageFileId = MostRecent.PageFileId
			AND PFR.Revision < MostRecent.Revision

		PRINT 'Deleted ' + Cast(@@rowcount as varchar) + ' file revisions.'

		DELETE FROM PageFileRevision WHERE PageFileId NOT IN (SELECT PageFileId FROM PageRevisionAttachment)
		DELETE FROM PageFile WHERE Id NOT IN (SELECT PageFileId FROM PageRevisionAttachment)

		PRINT 'Deleted ' + Cast(@@rowcount as varchar) + ' orphaned files.'

		UPDATE [Page] SET Revision = 1
		UPDATE PageRevision SET Revision = 1
		UPDATE PageRevisionAttachment SET PageRevision = 1
		UPDATE PageRevisionAttachment SET FileRevision = 1
		UPDATE PageFileRevision SET Revision = 1
		UPDATE PageFile SET Revision = 1


		COMMIT TRANSACTION
	END ELSE BEGIN--IF
		PRINT 'Not confirmed. Pass ''YES'' to delete all but the most recent page revisions ans reset all revisions to 1.'
	END--IF

END --PROCEDURE