SET NOCOUNT ON;

DECLARE @Text TABLE (Id Int NOT NULL IDENTITY(1,1), [Text] NVARCHAR(MAX))
DECLARE @EmojiId INT
DECLARE @Name as nvarchar(128)
DECLARE @Category as nvarchar(256)
DECLARE @Categories as nvarchar(2000) = ''
DECLARE @ImageData VarBinary(MAX)
DECLARE @MimeType nvarchar(50)

DECLARE Pages CURSOR FAST_FORWARD FOR
	SELECT
		[Id],
		[Name],
		[ImageData],
		[MimeType]
	FROM
		[Emoji]
OPEN Pages
FETCH FROM Pages INTO @EmojiId, @Name, @ImageData, @MimeType

WHILE(@@FETCH_STATUS = 0)
BEGIN--WHILE
	--------------------------------------------------------------------------------------------------

	SET @Categories = ''

	DECLARE Categories CURSOR FAST_FORWARD FOR
		SELECT
			[Category]
		FROM
			[EmojiCategory]
		WHERE
			EmojiId = @EmojiId
	OPEN Categories
	FETCH FROM Categories INTO @Category

	WHILE(@@FETCH_STATUS = 0)
	BEGIN--WHILE
		--------------------------------------------------------------------------------------------------

		SET @Categories = @Categories + @Category

		--------------------------------------------------------------------------------------------------
		FETCH FROM Categories INTO @Category

		IF(@@FETCH_STATUS = 0)
		BEGIN--IF
			SET @Categories = @Categories + ',' 
		END--IF

	END--WHILE

	CLOSE Categories DEALLOCATE Categories

	INSERT INTO @Text SELECT 'DECLARE @Discard TABLE (Id INT)'
	INSERT INTO @Text SELECT 'INSERT INTO @Discard EXEC SaveEmoji @Name=''' + @Name + ''', @Categories= ''' + @Categories + ''''
	INSERT INTO @Text SELECT 'DECLARE @EmojiId INT = (SELECT Id FROM Emoji WHERE Name = ''' + @Name + ''')'
	INSERT INTO @Text SELECT 'DECLARE @Data varbinary(max) = DECOMPRESS(' + Convert(varchar(MAX), @ImageData, 1) + ')'
	INSERT INTO @Text SELECT 'EXEC UpdatEmojiImage @EmojiId=@EmojiId, @ImageData=@Data, @MimeType= ''' + @MimeType + ''''
	INSERT INTO @Text SELECT 'GO'

	--------------------------------------------------------------------------------------------------
	FETCH FROM Pages INTO @EmojiId, @Name, @ImageData, @MimeType
END--WHILE

CLOSE Pages DEALLOCATE Pages

SELECT [Text] FROM @Text ORDER BY Id
