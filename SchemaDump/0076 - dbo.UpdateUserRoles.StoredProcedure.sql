IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpdateUserRoles]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpdateUserRoles] AS'
END
GO



ALTER PROCEDURE [dbo].[UpdateUserRoles]
(
	@UserId as int,
	@Roles VarChar(500)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	DELETE FROM
		UserRole
	WHERE
		UserId = @UserId

	INSERT INTO UserRole
	(
		UserId,
		RoleId
	)
	SELECT
		@UserId,
		R.Id
	FROM
		STRING_SPLIT(@Roles ,',') as SP
	INNER JOIN [Role] as R
		ON R.[Name] = SP.[value]
	WHERE
		IsNull(SP.[value], '') <> ''

END--PROCEDURE