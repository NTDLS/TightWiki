SELECT
	(SELECT Count(0) FROM [PageToken]) as Words,
	(SELECT Count(0) FROM [Page]) as Pages,
	(SELECT Count(0) FROM [PageTag]) as Tags,
	(SELECT Count(0) FROM [User]) as Users,
	(SELECT Count(0) FROM [PageRevision]) as PageRevisions,
	(SELECT Count(0) FROM [PageFile]) as Attachments,
	(SELECT SUM(DATALENGTH(Body)) FROM [PageRevision]) / 1024.0 / 1024.0 as TotalPageSizeMB,
	(SELECT SUM(Size) FROM [PageFileRevision]) / 1024.0 / 1024.0 as TotalAttachmentSizeMB
