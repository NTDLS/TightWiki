IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageInfoByTokens]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageInfoByTokens] AS'
END
GO



ALTER PROCEDURE [dbo].[GetPageInfoByTokens]
(
	@Tokens nVarChar(MAX)
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
		P.ModifiedDate,
		PT.[Weight]
	FROM
		PageToken as PT
	INNER JOIN STRING_SPLIT(@Tokens ,',') as SP
		ON PT.Token = SP.[value]
	INNER JOIN [Page] as P
		ON P.Id = PT.PageId

END--PROCEDURE