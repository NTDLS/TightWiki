IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpsertPageFile]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpsertPageFile] AS'
END
GO

ALTER PROCEDURE [dbo].[UpsertPageFile]
(
	@PageId int,
	@Name  nvarchar(500),
	@ContentType nvarchar(100),
	@Size int,
	@CreatedDate DateTime,
	@Data varbinary(max)
) AS
BEGIN--PROCEDURE

	SET NOCOUNT ON;

	DECLARE @Id INT = (SELECT Id FROM [PageFile] WHERE PageId = @PageId AND Name = @Name)

	IF @Id IS NULL
	BEGIN--IF
		INSERT INTO [dbo].[PageFile]
		(
			[PageId],
			[Name],
			[ContentType],
			[Size],
			[CreatedDate],
			[Data]
		)
		SELECT
			@PageId,
			@Name,
			@ContentType,
			@Size,
			@CreatedDate,
			@Data
	END ELSE BEGIN--IF
		UPDATE
			[dbo].[PageFile]
		SET
			ContentType = @ContentType,
			Size = @Size,
			CreatedDate = @CreatedDate,
			[Data] = @Data
		WHERE
			Id = @Id
	END--IF
END--PROCEDURE