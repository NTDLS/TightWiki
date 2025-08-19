DELETE FROM AccountRole
WHERE
	RoleId = @Id
	AND RoleId NOT IN (SELECT R.Id FROM [Role] as R WHERE R.Id = @Id AND R.IsBuiltIn = 1);

DELETE FROM RolePermission
WHERE
	RoleId = @Id
	AND RoleId NOT IN (SELECT R.Id FROM [Role] as R WHERE R.Id = @Id AND R.IsBuiltIn = 1);

DELETE FROM [Role]
WHERE
	Id = @Id
	AND Id NOT IN (SELECT R.Id FROM [Role] as R WHERE R.Id = @Id AND R.IsBuiltIn = 1)
