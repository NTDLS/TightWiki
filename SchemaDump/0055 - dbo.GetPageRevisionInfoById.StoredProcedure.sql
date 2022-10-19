IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageRevisionInfoById]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageRevisionInfoById] AS'
END
GO



ALTER PROCEDURE [dbo].[GetPageRevisionInfoById]
(
	@PageId Int,
	@Revision int = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		P.Id,
		P.[Name],
		P.[Description],
		PR.Revision,
		P.Navigation,
		P.CreatedByUserId,
		Createduser.AccountName as CreatedByUserName,
		P.CreatedDate,
		PR.ModifiedByUserId,
		ModifiedUser.AccountName as ModifiedByUserName,
		PR.ModifiedDate
	FROM
		[Page] as P
	INNER JOIN [PageRevision] as PR
		ON PR.PageId = P.Id
	INNER JOIN [User] as ModifiedUser
		ON ModifiedUser.Id = PR.ModifiedByUserId
	INNER JOIN [User] as Createduser
		ON Createduser.Id = P.CreatedByUserId
	WHERE
		P.Id = @PageId
		AND PR.Revision = IsNull(@Revision, P.Revision)

END--PROCEDURE