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

		UPDATE [Page] SET Revision = 1
		UPDATE PageRevision SET Revision = 1

		COMMIT TRANSACTION
	END ELSE BEGIN--IF
		PRINT 'Not confirmed. Pass ''YES'' to delete all but the most recent page revisions ans reset all revisions to 1.'
	END--IF

END --PROCEDURE