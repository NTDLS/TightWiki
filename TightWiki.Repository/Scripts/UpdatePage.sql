UPDATE
	Page
SET
	Description = @Description,
	Name = @Name,
	Namespace = @Namespace,
	Navigation = @Navigation,
	ModifiedByUserId = @ModifiedByUserId,
	ModifiedDate = @ModifiedDate
WHERE
	Id = @PageId
