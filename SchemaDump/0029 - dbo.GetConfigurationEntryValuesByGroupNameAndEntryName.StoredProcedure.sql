IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetConfigurationEntryValuesByGroupNameAndEntryName]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetConfigurationEntryValuesByGroupNameAndEntryName] AS'
END
GO

ALTER PROCEDURE [dbo].[GetConfigurationEntryValuesByGroupNameAndEntryName]
(
	@GroupName nVarchar(128),
	@entryName nVarchar(128)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
		
	SELECT
		CE.[Id] as [Id],
		CE.[ConfigurationGroupId] as [ConfigurationGroupId],
		CE.[Name] as [Name],
		CE.[Value] as [Value],
		CE.[Description] as [Description]
	FROM
		[ConfigurationEntry] as CE
	INNER JOIN [ConfigurationGroup] as CG
		ON CG.Id = CE.ConfigurationGroupId
	WHERE
		CG.[Name] = @GroupName
		AND CE.[Name] = @entryName

END--PROCEDURE