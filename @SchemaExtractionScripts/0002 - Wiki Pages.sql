SET NOCOUNT ON;
--DECLARE @NamespaceToDump nVarChar(128) = 'Wiki Help'

DECLARE @Text TABLE (Id Int NOT NULL IDENTITY(1,1), [Text] NVARCHAR(MAX))
DECLARE @Name as nvarchar (128)
DECLARE @Namespace as nvarchar (128)
DECLARE @Navigation as nvarchar (128)
DECLARE @Description as nvarchar (MAX)
DECLARE @Body as varbinary (MAX)
DECLARE @Max as nvarchar (MAX)

DECLARE Pages CURSOR FAST_FORWARD FOR
	SELECT
		P.[Name],
		P.[Namespace],
		P.Navigation,
		P.[Description],
		COMPRESS(PR.Body)
	FROM
		[Page] as P
	INNER JOIN PageRevision as PR
		ON PR.PageId = P.Id
		AND PR.Revision = P.Revision
	--WHERE
		--P.[Namespace] = @NamespaceToDump
OPEN Pages
FETCH FROM Pages INTO @Name, @Namespace, @Navigation, @Description, @Body

WHILE(@@FETCH_STATUS = 0)
BEGIN--WHILE
	--------------------------------------------------------------------------------------------------

	INSERT INTO @Text SELECT 'DECLARE @UserId INT = (SELECT Id FROM [User] WHERE AccountName = ''admin'')'
	INSERT INTO @Text SELECT 'DECLARE @DateTime DateTime = GetUtcDate()'
	INSERT INTO @Text SELECT 'DECLARE @Id as int = (SELECT Id FROM [Page] WHERE Navigation = ''' + @Navigation + ''')'
	INSERT INTO @Text SELECT 'DECLARE @Name as nvarchar (128) = ''' + @Name + ''''
	IF(@Namespace IS NULL)
	INSERT INTO @Text SELECT 'DECLARE @Namespace as nvarchar (128) = NULL'
	ELSE
	INSERT INTO @Text SELECT 'DECLARE @Namespace as nvarchar (128) = ''' + @Namespace + ''''
	INSERT INTO @Text SELECT 'DECLARE @Navigation as nvarchar (128) = ''' + @Navigation + ''''
	INSERT INTO @Text SELECT 'DECLARE @Description as nvarchar (MAX) = ''' + @Description + ''''

	IF @Body IS NULL OR LEN(@Body) = 0
	INSERT INTO @Text SELECT 'DECLARE @Body varbinary(max) = NULL'
	ELSE
	INSERT INTO @Text SELECT 'DECLARE @Body varbinary(max) = DECOMPRESS(' + Convert(nvarchar(MAX), @Body, 1) + ')'
	INSERT INTO @Text SELECT 'EXEC SavePage @Id = @Id, @Name = @Name, @Namespace = @Namespace, @Navigation = @Navigation, @Description = @Description, @Body = @Body, @CreatedByUserId = @UserId, @CreatedDate = @DateTime, @ModifiedByUserId = @UserId, @ModifiedDate = @DateTime'
	INSERT INTO @Text SELECT 'GO'

	--------------------------------------------------------------------------------------------------
	FETCH FROM Pages INTO @Name, @Namespace, @Navigation, @Description, @Body
END--WHILE

CLOSE Pages DEALLOCATE Pages

SELECT [Text] FROM @Text ORDER BY Id
