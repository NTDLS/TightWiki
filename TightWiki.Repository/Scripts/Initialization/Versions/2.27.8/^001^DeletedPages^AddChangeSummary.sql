--##IF COLUMN NOT EXISTS(PageRevision, ChangeSummary)

ALTER TABLE PageRevision ADD ChangeSummary TEXT NULL;
