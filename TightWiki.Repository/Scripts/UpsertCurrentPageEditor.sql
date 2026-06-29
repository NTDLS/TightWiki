INSERT INTO CurrentPageEditors (PageId, UserId, AccountName, UTCDate)
VALUES (@PageId, @UserId, @AccountName, @UTCDate)
ON CONFLICT(PageId, UserId) DO UPDATE SET AccountName = excluded.AccountName, UTCDate = excluded.UTCDate;
