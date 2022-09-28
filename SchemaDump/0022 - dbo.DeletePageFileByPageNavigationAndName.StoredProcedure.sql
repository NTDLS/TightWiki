IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[DeletePageFileByPageNavigationAndName]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[DeletePageFileByPageNavigationAndName] AS'
END
GO

ALTER PROCEDURE [dbo].[DeletePageFileByPageNavigationAndName]
(
	@PageNavigation nVarChar(128),
    @FileName nVarChar(500)
) AS
BEGIN--PROCEDURE


	DELETE
		PF
	FROM		
		[dbo].[PageFile] as PF
	INNER JOIN [dbo].[Page] as P
		ON P.Id = PF.PageId
	WHERE
		P.Navigation = @PageNavigation
		AND PF.[Name] = @FileName
		
END--PROCEDURE