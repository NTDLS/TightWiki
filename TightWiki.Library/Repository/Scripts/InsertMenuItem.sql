INSERT INTO MenuItem
(
	Name,
	Link,
	Ordinal
)
SELECT
	@Name,
	@Link,
	@Ordinal;

SELECT last_insert_rowid();
