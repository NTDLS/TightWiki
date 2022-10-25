IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageInfoByNavigation]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageInfoByNavigation] AS'
END
GO




ALTER PROCEDURE [dbo].[GetPageInfoByNavigation]
(
	@Navigation nVarChar(128)
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		P.Id,
		P.[Name],
		P.[Description],
		P.Navigation,
		P.CreatedByUserId,
		P.CreatedDate,
		P.ModifiedByUserId,
		P.ModifiedDate
	FROM
		[Page] as P
	WHERE
		P.Navigation = @Navigation

END--PROCEDURE