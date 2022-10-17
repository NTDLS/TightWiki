IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpsertPageFile]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpsertPageFile] AS'
END
GO

ALTER PROCEDURE [dbo].[UpsertPageFile]
(
	@PageId int,
	@Name nvarchar(250),
	@FileNavigation nvarchar(250),
	@ContentType nvarchar(250),
	@Size int,
	@CreatedDate DateTime,
	@Data varbinary(max)
) AS
BEGIN--PROCEDURE

	SET NOCOUNT ON;

	BEGIN TRANSACTION

	select * from PageFile

	DECLARE @PageFileId INT = (SELECT Id FROM [PageFile] WHERE PageId = @PageId AND Navigation = @FileNavigation)
	DECLARE @FileRevision INT
	DECLARE @PageRevision INT

	IF @PageFileId IS NULL
	BEGIN--IF
		INSERT INTO [dbo].[PageFile]
		(
			[PageId],
			[Name],
			[Navigation],
			[CreatedDate],
			[Revision]
		)
		SELECT
			@PageId,
			@Name,
			@FileNavigation,
			@CreatedDate,
			0

		SET @PageFileId = cast(SCOPE_IDENTITY() as int)
	END--IF
	
	UPDATE [PageFile] SET Revision = Revision + 1 WHERE Id = @PageFileId
	SELECT @FileRevision = Revision FROM [PageFile] WHERE Id = @PageFileId
	SELECT @PageRevision = Revision FROM [Page] WHERE Id = @PageId

	INSERT INTO PageFileRevision
	(
		PageFileId,
		ContentType,
		Size,
		CreatedDate,
		[Data],
		Revision
	)
	SELECT
		@PageFileId,
		@ContentType,
		@Size,
		GETUTCDATE(),
		@Data,
		@FileRevision

	INSERT INTO PageRevisionAttachment
	(
		PageId,
		PageFileId,
		FileRevision,
		PageRevision
	)
	SELECT
		@PageId,
		@PageFileId,
		@FileRevision,
		@PageRevision

	COMMIT TRANSACTION

END--PROCEDURE