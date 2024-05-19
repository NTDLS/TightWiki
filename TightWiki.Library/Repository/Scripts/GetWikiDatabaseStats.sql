SELECT
	(SELECT Count(0) FROM [Page]) as Pages,
	(SELECT Count(0) FROM [PageReference]) as IntraLinks,
	(SELECT Count(0) FROM PageRevision) as PageRevisions,
	(SELECT Count(0) FROM PageFile) as PageAttachments,
	(SELECT Count(0) FROM PageFileRevision) as PageAttachmentRevisions,
	(SELECT Count(0) FROM PageTag) as PageTags,
	(SELECT Count(0) FROM PageToken) as PageSearchTokens,
	(SELECT Count(0) FROM Profile) as Profiles,
	(SELECT Count(0) FROM AspNetUsers) as Users
