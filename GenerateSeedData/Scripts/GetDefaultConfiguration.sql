SELECT
	CG.Name as ConfigurationGroupName,
	CE.Name as ConfigurationEntryName,
	CE.Value,
	CE.DataTypeId,
	CE.Description,
	CE.IsEncrypted,
	CE.IsRequired
FROM
	ConfigurationEntry as CE
INNER JOIN ConfigurationGroup as CG
	ON CG.Id = CE.ConfigurationGroupId
