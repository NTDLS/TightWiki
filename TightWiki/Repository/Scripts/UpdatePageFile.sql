SELECT @FileRevision = Revision FROM [PageFile] WHERE Id = @PageFileId
DECLARE @DataHash INT = CHECKSUM(@Data)

--IF NOT EXISTS(SELECT TOP 1 1 FROM PageFileRevision WHERE PageFileId = @PageFileId AND Revision = @FileRevision AND DataHash = @DataHash)
--BEGIN--IF
--SET @FileRevision = @FileRevision + 1

UPDATE [PageFile] SET Revision = @FileRevision WHERE Id = @PageFileId
SELECT @PageRevision = Revision FROM [Page] WHERE Id = @PageId

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
	GETUTCDATE(),
	@Data,
	@FileRevision,
	@DataHash

INSERT INTO PageRevisionAttachment
(
	PageId,
	PageFileId,
	FileRevision,
	PageRevision
)
SELECT
	@PageId,
	@PageFileId,
	@FileRevision,
	@PageRevision
