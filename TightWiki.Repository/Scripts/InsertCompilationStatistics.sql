INSERT INTO CompilationStatistics
(
	[PageId],
	[CreatedDate],
	[WikifyTimeMs],
	[MatchCount],
	[ErrorCount],
	[OutgoingLinkCount],
	[TagCount],
	[ProcessedBodySize],
	[BodySize]
)
SELECT
	@PageId,
	@CreatedDate,
	@WikifyTimeMs,
	@MatchCount,
	@ErrorCount,
	@OutgoingLinkCount,
	@TagCount,
	@ProcessedBodySize,
	@BodySize
