INSERT INTO [PageRevision]
(
	PageId,
	[Name],
	[Namespace],
	[Description],
	Body,
	DataHash,
	Revision,
	ModifiedByUserId,
	ModifiedDate
)
SELECT
	@PageId,
	@Name,
	@Namespace,
	@Description,
	@Body,
	@DataHash,
	@PageRevision,
	@ModifiedByUserId,
	@ModifiedDate
FROM
	[Page]
WHERE
	Id = @PageId;
