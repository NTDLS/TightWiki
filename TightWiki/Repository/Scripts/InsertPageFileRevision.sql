--Insert the actual file data.
INSERT INTO PageFileRevision
(
	PageFileId,
	ContentType,
	Size,
	CreatedDate,
	[Data],
	Revision,
	DataHash
)
SELECT
	@PageFileId,
	@ContentType,
	@Size,
	@CreatedDate,
	@Data,
	@FileRevision,
	@DataHash;
