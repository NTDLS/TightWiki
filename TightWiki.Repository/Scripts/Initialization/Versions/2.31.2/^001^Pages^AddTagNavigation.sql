--##IF COLUMN NOT EXISTS(PageTag, Navigation)

ALTER TABLE PageTag ADD Navigation TEXT NOT NULL COLLATE NOCASE DEFAULT('');

UPDATE PageTag SET Navigation = Replace(Replace(Replace(Tag, '.', '_'), '::', '_'), ' ', '_');
