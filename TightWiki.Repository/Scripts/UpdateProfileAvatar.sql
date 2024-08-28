UPDATE
	Profile
SET
	Avatar = @Avatar,
	AvatarContentType = @ContentType
WHERE
	UserId = @UserId
