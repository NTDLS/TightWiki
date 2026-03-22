INSERT INTO FeatureTemplate
(
	Name,
	Type,
	PageId,
	Description,
	TemplateText
)
SELECT
	@Name,
	@Type,
	(SELECT Id FROM Page WHERE Name = @PageName LIMIT 1),
	@Description,
	@TemplateText
ON CONFLICT(Name, Type) DO UPDATE
SET
	Description = @Description,
	TemplateText = @TemplateText,
	PageId = (SELECT Id FROM Page WHERE Name = @PageName LIMIT 1);
