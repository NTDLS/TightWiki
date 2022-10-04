IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageInfoByTags]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageInfoByTags] AS'
END
GO

ALTER PROCEDURE [dbo].[GetPageInfoByTags]
(
	@Tags nVarChar(MAX)
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT DISTINCT
		P.Id,
		P.[Name],
		P.[Description],
		P.Navigation,
		P.CreatedByUserId,
		P.CreatedDate,
		P.ModifiedByUserId,
		P.ModifiedDate
	FROM
		PageTag as PT
	INNER JOIN STRING_SPLIT(@Tags ,',') as SP
		ON PT.Tag = SP.[value]
	INNER JOIN [Page] as P
		ON P.Id = PT.PageId

END--PROCEDURE