IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetAssociatedTags]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetAssociatedTags] AS'
END
GO



ALTER PROCEDURE [dbo].[GetAssociatedTags]
(
	@Tag nvarchar(128)	
) AS
BEGIN--PROCEDURE
	SELECT TOP 100
		[Extent].[Tag],
		Count(DISTINCT [Extent].PageId) as [PageCount]
	FROM
		PageTag as [Root]
	INNER JOIN PageTag as [Interm]
		ON [Interm].[Tag] = [Root].[Tag]
		AND [Interm].[PageId] = [Root].[PageId]
	INNER JOIN PageTag as [Extent]
		ON [Extent].[PageId] = [Interm].[PageId]
	WHERE
		[Root].[Tag] = @Tag
	GROUP BY
		[Extent].[Tag]
END--PROCEDURE