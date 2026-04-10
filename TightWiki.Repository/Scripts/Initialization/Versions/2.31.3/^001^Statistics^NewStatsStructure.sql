--##IF TABLE NOT EXISTS(PageStatistics)

ALTER TABLE CompilationStatistics RENAME TO PageStatistics;

ALTER TABLE PageStatistics ADD "TotalViewCount" INTEGER NOT NULL DEFAULT 0;
