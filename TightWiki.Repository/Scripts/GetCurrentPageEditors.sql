SELECT AccountName
FROM CurrentPageEditors
WHERE PageId = @PageId
  AND UTCDate >= @ThresholdDate;
