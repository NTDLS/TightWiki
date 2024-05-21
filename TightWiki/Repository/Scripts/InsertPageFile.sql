INSERT INTO [PageFile]
(
	[PageId],
	[Name],
	[Navigation],
	[CreatedDate],
	[Revision]
)
SELECT
	@PageId,
	@Name,
	@FileNavigation,
	@CreatedDate,
	0
