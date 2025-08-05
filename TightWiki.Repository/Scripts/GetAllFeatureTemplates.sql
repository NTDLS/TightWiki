SELECT
	FT.Name,
	FT.Type,
	FT.PageId,
	FT.Description,
	FT.TemplateText,
	P.Navigation as HelpPageNavigation
FROM
	FeatureTemplate as FT
LEFT OUTER JOIN Page as P
	ON P.Id = FT.PageId