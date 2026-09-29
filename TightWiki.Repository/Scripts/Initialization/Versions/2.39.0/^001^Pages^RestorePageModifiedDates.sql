--Earlier versions stamped pages as modified whenever they were saved without changes, including when the
--  help pages were re-seeded during every upgrade. Restore each page's modified date and user from its current revision.

UPDATE
	Page
SET
	ModifiedDate = (SELECT PR.ModifiedDate FROM PageRevision as PR WHERE PR.PageId = Page.Id AND PR.Revision = Page.Revision),
	ModifiedByUserId = (SELECT PR.ModifiedByUserId FROM PageRevision as PR WHERE PR.PageId = Page.Id AND PR.Revision = Page.Revision)
WHERE
	EXISTS (SELECT 1 FROM PageRevision as PR WHERE PR.PageId = Page.Id AND PR.Revision = Page.Revision);
