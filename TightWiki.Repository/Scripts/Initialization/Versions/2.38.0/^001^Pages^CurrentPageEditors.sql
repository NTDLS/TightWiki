--##IF TABLE NOT EXISTS(CurrentPageEditors)

CREATE TABLE CurrentPageEditors (
    PageId      INTEGER NOT NULL,
    UserId      TEXT    NOT NULL,
    AccountName TEXT    NOT NULL,
    UTCDate     TEXT    NOT NULL,
    PRIMARY KEY (PageId, UserId)
);
