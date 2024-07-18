SELECT
	CE.[Id] as [Id],
	CE.[ConfigurationGroupId] as [ConfigurationGroupId],
	CE.[Name] as [Name],
	CE.[Value] as [Value],
	CE.[IsEncrypted] as [IsEncrypted],
	CE.[Description] as [Description]
FROM
	[ConfigurationEntry] as CE
INNER JOIN [ConfigurationGroup] as CG
	ON CG.Id = CE.ConfigurationGroupId
WHERE
	CG.[Name] = @GroupName
