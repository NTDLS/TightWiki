INSERT INTO [PageRevision]
(
	PageId,
	[Name],
	[Namespace],
	[Description],
	Body,
	DataHash,
	Revision,
	ChangeSummary,
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
	@ChangeSummary,
	@ModifiedByUserId,
	@ModifiedDate
FROM
	[Page]
WHERE
	Id = @PageId;
