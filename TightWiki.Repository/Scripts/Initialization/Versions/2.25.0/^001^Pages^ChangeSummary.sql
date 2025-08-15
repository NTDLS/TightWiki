--##IF COLUMN NOT EXISTS(PageRevision, ChangeSummary)

ALTER TABLE [PageRevision] ADD COLUMN [ChangeSummary] TEXT NULL;
