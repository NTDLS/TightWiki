INSERT INTO Page
(
	Name,
	Namespace,
	Description,
	Navigation,
	Revision,
	CreatedByUserId,
	CreatedDate,
	ModifiedByUserId,
	ModifiedDate
)
VALUES
(
	@Name,
	@Namespace,
	@Description,
	@Navigation,
	1,
	@CreatedByUserId,
	@CreatedDate,
	@ModifiedByUserId,
	@ModifiedDate
);

SELECT last_insert_rowid();
