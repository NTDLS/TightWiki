--Insert the actual file data.
INSERT INTO PageFileRevision
(
	PageFileId,
	ContentType,
	Size,
	CreatedDate,
	CreatedByUserId,
	[Data],
	Revision,
	DataHash
)
SELECT
	@PageFileId,
	@ContentType,
	@Size,
	@CreatedDate,
	@CreatedByUserId,
	@Data,
	@FileRevision,
	@DataHash;
