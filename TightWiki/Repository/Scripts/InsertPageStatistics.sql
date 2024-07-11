INSERT INTO ComplicationStatistics
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
	datetime('now', 'utc'),
	@WikifyTimeMs,
	@MatchCount,
	@ErrorCount,
	@OutgoingLinkCount,
	@TagCount,
	@ProcessedBodySize,
	@BodySize
