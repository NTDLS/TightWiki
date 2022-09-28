IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[SavePage]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[SavePage] AS'
END
GO


ALTER PROCEDURE [dbo].[SavePage]
(
	@Id as int = NULL,
	@Name as nvarchar (128),
	@Navigation as nvarchar (128),
	@Description as nvarchar (MAX),
	@Body as nvarchar (MAX),
	@CreatedByUserId as int,
	@CreatedDate as datetime,
	@ModifiedByUserId as int,
	@ModifiedDate as datetime
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	DECLARE @PageId INT = (SELECT Id FROM [Page] WHERE Id = @Id)

	IF(@PageId IS NULL)
	BEGIN--IF
		INSERT INTO [Page]
		(
			[Name],
			[Navigation],
			[Description],
			[Body],
			[CreatedByUserId],
			[CreatedDate],
			[ModifiedByUserId],
			[ModifiedDate]
		)
		VALUES
		(
			@Name,
			@Navigation,
			@Description,
			@Body,
			@CreatedByUserId,
			GETUTCDATE(),
			@ModifiedByUserId,
			GETUTCDATE()
		)

		SET @PageId = cast(SCOPE_IDENTITY() as int)
	END ELSE BEGIN--IF

		INSERT INTO [PageHistory]
		(
			PageId,
			[Name],
			[Description],
			Body,
			ModifiedByUserId,
			ModifiedDate
		)
		SELECT
			Id,
			[Name],
			[Description],
			Body,
			@ModifiedByUserId,
			GETUTCDATE()
		FROM
			[Page]
		WHERE
			Id = @Id			

		UPDATE
			[Page]
		SET
			[Name] = @Name,
			[Navigation] = @Navigation,
			[Description] = @Description,
			[Body] = @Body,
			[CreatedByUserId] = @CreatedByUserId,
			[CreatedDate] = @CreatedDate,
			[ModifiedByUserId] = @ModifiedByUserId,
			[ModifiedDate] = @ModifiedDate
		FROM
			[Page]
		WHERE
			Id = @Id

	END--IF

	SELECT @PageId

END--PROCEDURE