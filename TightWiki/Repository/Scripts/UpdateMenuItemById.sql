UPDATE
	MenuItem
SET
	Id = @Id,
	Name = @Name,
	Link = @Link,
	Ordinal = @Ordinal
WHERE
	Id = @Id
