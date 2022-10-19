IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetUserRolesByUserId]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetUserRolesByUserId] AS'
END
GO



ALTER PROCEDURE [dbo].[GetUserRolesByUserId]
(
	@UserID int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		R.Id,
		R.[Name]
	FROM
		UserRole as UR
	INNER JOIN [Role] as R
		ON R.Id = UR.RoleId
	WHERE
		UserId = @UserID

END--PROCEDURE