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

	BEGIN TRANSACTION

	DECLARE @PageId INT = (SELECT Id FROM [Page] WHERE Id = @Id)
	DECLARE @PageRevision INT

	IF(@PageId IS NULL)
	BEGIN--IF
		INSERT INTO [Page]
		(
			[Name],
			[Navigation],
			[Revision],
			[CreatedByUserId],
			[CreatedDate],
			[ModifiedByUserId],
			[ModifiedDate]
		)
		VALUES
		(
			@Name,
			@Navigation,
			0,
			@CreatedByUserId,
			GETUTCDATE(),
			@ModifiedByUserId,
			GETUTCDATE()
		)

		SET @PageId = cast(SCOPE_IDENTITY() as int)
	END ELSE BEGIN--IF

		UPDATE
			[Page]
		SET
			[Description] = @Description,
			[Name] = @Name,
			[Navigation] = @Navigation,
			[ModifiedByUserId] = @ModifiedByUserId,
			[ModifiedDate] = GETUTCDATE()
		WHERE
			[Id] = @Id

		SET @PageId = @Id
	END--IF

	UPDATE [Page] SET Revision = Revision + 1 WHERE Id = @PageId
	SELECT @PageRevision = Revision FROM [Page] WHERE Id = @PageId
	
	INSERT INTO [PageRevision]
	(
		PageId,
		[Name],
		[Description],
		Body,
		Revision,
		ModifiedByUserId,
		ModifiedDate
	)
	SELECT
		@PageId,
		@Name,
		@Description,
		@Body,
		@PageRevision,
		@ModifiedByUserId,
		GETUTCDATE()
	FROM
		[Page]
	WHERE
		Id = @PageId

	INSERT INTO PageRevisionAttachment
	(
		PageId,
		PageFileId,
		FileRevision,
		PageRevision
	)
	SELECT
		PRA.PageId,
		PRA.PageFileId,
		PRA.FileRevision,
		PRA.PageRevision + 1
	FROM
		PageRevisionAttachment as PRA
	INNER JOIN PageFile as PF
		ON PF.Id = PRA.PageFileId
		AND PF.Revision = PRA.FileRevision
	WHERE
		PRA.PageId = @PageId
		AND PRA.PageRevision = @PageRevision - 1


	SELECT @PageId as PageId

	COMMIT TRANSACTION

END--PROCEDURE