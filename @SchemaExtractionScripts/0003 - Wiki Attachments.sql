SET NOCOUNT ON;
--DECLARE @NamespaceToDump nVarChar(128) = 'Wiki Help'

DECLARE @Text TABLE (Id Int NOT NULL IDENTITY(1,1), [Text] NVARCHAR(MAX))
DECLARE @Name nvarchar(250)
DECLARE @FileNavigation nvarchar(250)
DECLARE @PageNavigation nvarchar(250)
DECLARE @ContentType nvarchar(250)
DECLARE @Size int
DECLARE @Data varbinary(max)
DECLARE @Max as nvarchar (MAX)

DECLARE Files CURSOR FAST_FORWARD FOR
	SELECT
		PF.[Name] as [Name],
		PF.Navigation as FileNavigation,
		P.Navigation as PageNavigation,
		PFR.[ContentType] as [ContentType],
		PFR.[Size] as [Size],
		COMPRESS(PFR.[Data]) as [Data]
	FROM
		[PageFile] as PF
	INNER JOIN [Page] as P
		ON P.Id = PF.PageId
	INNER JOIN [PageRevision] as PR
		ON PR.PageId = P.Id
	INNER JOIN PageRevisionAttachment as PRA
		ON PRA.PageId = P.Id
		AND PRA.PageFileId = PF.Id
		AND PRA.PageRevision = PR.Revision
		AND PRA.FileRevision = PF.Revision --Latest file revision.
	INNER JOIN PageFileRevision as PFR
		ON PFR.PageFileId = PF.Id
		AND PFR.Revision = PRA.FileRevision
	WHERE
		PR.Revision = P.Revision
		--AND P.[Namespace] = @NamespaceToDump
OPEN Files
FETCH FROM Files INTO @Name, @FileNavigation, @PageNavigation, @ContentType, @Size, @Data

WHILE(@@FETCH_STATUS = 0)
BEGIN--WHILE
	--------------------------------------------------------------------------------------------------

	INSERT INTO @Text SELECT 'DECLARE @PageId int = (SELECT Id FROM [Page] WHERE Navigation = ''' + @PageNavigation + ''')'
	INSERT INTO @Text SELECT 'DECLARE @Name nvarchar(250) = ''' + @Name +''''
	INSERT INTO @Text SELECT 'DECLARE @FileNavigation nvarchar(250) = ''' + @FileNavigation +''''
	INSERT INTO @Text SELECT 'DECLARE @ContentType nvarchar(250) = ''' + @ContentType +''''
	INSERT INTO @Text SELECT 'DECLARE @Size int = ' + Convert(VarChar, @Size)
	INSERT INTO @Text SELECT 'DECLARE @CreatedDate DateTime = GetUTCDate()'
	INSERT INTO @Text SELECT 'DECLARE @Data varbinary(max) = DECOMPRESS(' + Convert(varchar(MAX), @Data, 1) + ')'
	INSERT INTO @Text SELECT 'EXEC UpsertPageFile @PageId = @PageId, @Name = @Name, @FileNavigation = @FileNavigation, @ContentType = @ContentType, @Size = @Size, @CreatedDate = @CreatedDate, @Data = @Data'
	INSERT INTO @Text SELECT 'GO'
	--------------------------------------------------------------------------------------------------
	FETCH FROM Files INTO @Name, @FileNavigation, @PageNavigation, @ContentType, @Size, @Data
END--WHILE

CLOSE Files DEALLOCATE Files

SELECT [Text] FROM @Text ORDER BY Id
