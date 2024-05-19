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
ORDER BY
	CG.[Name],
	CE.[Name]
