SELECT
	FT.Name,
	FT.Type,
	P.Name as PageName,
	FT.Description,
	FT.TemplateText
FROM
	FeatureTemplate as FT
LEFT JOIN Page as P
	ON P.Id = FT.PageId
