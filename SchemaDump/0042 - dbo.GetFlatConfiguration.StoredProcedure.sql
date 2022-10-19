IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetFlatConfiguration]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetFlatConfiguration] AS'
END
GO



ALTER PROCEDURE [dbo].[GetFlatConfiguration] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
		
	SELECT
		CG.Id as [GroupId],
		CG.[Name] as [GroupName],
		CG.[Description] as [GroupDescription],

		CE.[Id] as [EntryId],
		CE.[Name] as [EntryName],
		CE.[Value] as [EntryValue],
		CE.[Description] as [EntryDescription],
		CE.IsEncrypted,

		DT.[Name] as DataType
	FROM
		[ConfigurationEntry] as CE
	INNER JOIN [ConfigurationGroup] as CG
		ON CG.Id = CE.ConfigurationGroupId
	INNER JOIN DataType as DT
		ON DT.Id = CE.DataTypeId

END--PROCEDURE