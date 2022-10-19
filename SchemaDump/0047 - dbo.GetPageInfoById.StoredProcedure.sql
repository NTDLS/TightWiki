IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageInfoById]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageInfoById] AS'
END
GO



ALTER PROCEDURE [dbo].[GetPageInfoById]
(
	@PageId int
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		P.Id,
		P.[Name],
		P.[Description],
		P.Navigation,
		P.Revision,
		P.CreatedByUserId,
		P.CreatedDate,
		P.ModifiedByUserId,
		P.ModifiedDate
	FROM
		[Page] as P
	WHERE
		P.Id = @PageId

END--PROCEDURE