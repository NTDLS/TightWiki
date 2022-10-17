IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[SaveConfigurationEntryValueByGroupAndEntry]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[SaveConfigurationEntryValueByGroupAndEntry] AS'
END
GO

ALTER PROCEDURE [dbo].[SaveConfigurationEntryValueByGroupAndEntry]
(
	@GroupName nVarchar(128),
	@entryName nVarchar(128),
	@value nVarchar(max)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
		
	UPDATE
		CE
	SET
		[Value] = @value
	FROM
		[ConfigurationEntry] as CE
	INNER JOIN [ConfigurationGroup] as CG
		ON CG.Id = CE.ConfigurationGroupId
	WHERE
		CG.[Name] = @GroupName
		AND CE.[Name] = @entryName

END--PROCEDURE