--The ReferencesPageId is NULL by default and needs to be filled in for pages that referece orphaned pages.
UPDATE
	PageReference
SET
	ReferencesPageId = @PageId
WHERE
	ReferencesPageNavigation = @PageNavigation
